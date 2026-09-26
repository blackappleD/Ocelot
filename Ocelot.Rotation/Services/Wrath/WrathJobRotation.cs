using Dalamud.Plugin;
using WrathCombo.API;
using WrathCombo.API.Enum;

namespace Ocelot.Rotation.Services.Wrath;

public sealed class WrathJobRotation(
    IDalamudPluginInterface pluginInterface,
    OcelotPlugin plugin) : IJobRotationBackend, IDisposable
{
    /// <summary>Phantom-job options BOCCHI always leaves off, whatever the user's blacklist says.</summary>
    public static readonly IReadOnlySet<string> BuiltInOccultOptionsLeftOff = new HashSet<string>(StringComparer.Ordinal)
    {
        "Phantom_Chemist_OccultElixir",
        // Wrath has no useful Phantom BLM gate for this yet — bulk-enable spam-casts Toad.
        "Phantom_BlackMage_OccultToad",
    };

    private readonly Lock gate = new();

    private Lazy<Guid?> lease = NewLease(pluginInterface, plugin);

    private bool farmingDefaultsApplied;

    // Built-in left-off set plus the user's blacklist from the session options.
    private HashSet<string> occultOptionsLeftOff = new(BuiltInOccultOptionsLeftOff, StringComparer.Ordinal);

    private bool manualTargeting = true;

    private bool? rotationOn;

    private uint? trackedPhantomJobId;

    public JobRotationBackendKind Kind => JobRotationBackendKind.Wrath;

    private static Lazy<Guid?> NewLease(IDalamudPluginInterface pluginInterface, OcelotPlugin plugin)
    {
        return new Lazy<Guid?>(
            () =>
            {
                WrathIPCWrapper.Init(pluginInterface, WrathIPCWrapper.ErrorType.All);
                // Wrath checks the first argument against loaded plugins (LeaseePluginDisabled),
                // so it must be the real InternalName, not the display name.
                return WrathIPCWrapper.RegisterForLease(pluginInterface.InternalName, plugin.Name);
            },
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private Guid? Lease
    {
        get
        {
            lock (gate)
            {
                return lease.Value;
            }
        }
    }

    public void Prepare(JobRotationSessionOptions options)
    {
        manualTargeting = options.ManualTargeting;
        lock (gate)
        {
            occultOptionsLeftOff = BuildOccultOptionsLeftOff(options.DisabledOccultOptions);
        }

        if (!Lease.HasValue)
        {
            return;
        }

        // Wrath throws if configs are set before AutoRotationControlled[0] exists.
        SetAutoRotationState(false);
        rotationOn = false;
        EnsureCurrentJobReady();
        ApplyFarmingDefaults();
    }

    public void Enable(CombatActivity activity)
    {
        _ = activity;

        // Wrath suspends leases on job change without telling us; InvalidLease re-arms below.
        if (rotationOn == true && farmingDefaultsApplied && Lease.HasValue)
        {
            SetResult touch = WrathIPCWrapper.SetAutoRotationState(Lease.Value, true);
            if (touch != SetResult.InvalidLease)
            {
                return;
            }

            DropDeadLease();
        }

        if (!SetAutoRotationState(true))
        {
            return;
        }

        rotationOn = true;
        EnsureCurrentJobReady();
        ApplyFarmingDefaults();
    }

    public void Disable()
    {
        if (rotationOn == false)
        {
            return;
        }

        // Always clear local intent. Lease may already be dead (JobChanged); still try IPC.
        _ = SetAutoRotationState(false);
        rotationOn = false;
    }

    // Set job-ready once in Enable/Prepare. Re-calling every tick spams Wrath if the lease dies.
    public void Refresh()
    {
    }

    public void ClearAppliedCache() => rotationOn = null;

    public void SyncContentJob(uint? contentJobId)
    {
        if (!Lease.HasValue)
        {
            return;
        }

        lock (gate)
        {
            if (trackedPhantomJobId == contentJobId)
            {
                return;
            }

            uint? previous = trackedPhantomJobId;
            trackedPhantomJobId = contentJobId;

            // Release the job we are leaving or the lease keeps every phantom job enabled.
            if (previous is { } old && old != contentJobId)
            {
                TryReleaseOccult(Lease.Value, old);
            }

            if (contentJobId is { } next)
            {
                TryLockOccultOptimal(Lease.Value, next);
            }
        }
    }

    public void Teardown()
    {
        Disable();
        lock (gate)
        {
            trackedPhantomJobId = null;
        }

        ReleaseLease();
    }

    public void Dispose() => Teardown();

    /// <summary>
    ///     Set Auto-Rotation on/off. Recreates the lease once on InvalidLease (job change, etc.).
    ///     Does not change <see cref="rotationOn"/> — callers own that.
    /// </summary>
    private bool SetAutoRotationState(bool on)
    {
        return InvokeWithLeaseRecovery(id => WrathIPCWrapper.SetAutoRotationState(id, on));
    }

    private void ReleaseLease()
    {
        Lazy<Guid?> old;
        lock (gate)
        {
            old = lease;
            lease = NewLease(pluginInterface, plugin);
            farmingDefaultsApplied = false;
            rotationOn = null;
            trackedPhantomJobId = null;
        }

        if (old is { IsValueCreated: true, Value: { } id })
        {
            WrathIPCWrapper.ReleaseControl(id);
        }
    }

    /// <summary>
    ///     Drop a Guid Wrath already cancelled. Keep <see cref="rotationOn"/> so Enable/Disable can
    ///     re-apply intent on the new registration. Do not ReleaseControl (LeaseeReleased noise).
    /// </summary>
    private void DropDeadLease()
    {
        lock (gate)
        {
            lease = NewLease(pluginInterface, plugin);
            farmingDefaultsApplied = false;
            trackedPhantomJobId = null;
        }
    }

    private void EnsureCurrentJobReady()
    {
        // Best-effort: Auto-Rotation can be locked on even if job-ready fails briefly.
        _ = InvokeWithLeaseRecovery(WrathIPCWrapper.SetCurrentJobAutoRotationReady);
    }

    private void ApplyFarmingDefaults()
    {
        if (farmingDefaultsApplied)
        {
            return;
        }

        // On InvalidLease, restore Auto-Rotation on the new lease before configs (Wrath requires it).
        if (!InvokeWithLeaseRecovery(
                id => ApplyFarmingDefaultsTo(id),
                rearmAutoRotationAfterDrop: true))
        {
            return;
        }

        farmingDefaultsApplied = true;
    }

    private SetResult ApplyFarmingDefaultsTo(Guid id)
    {
        // Only force what Bocchi needs + a few safe combat defaults. Everything else
        // (rez policy, healer targeting, AoE count, etc.) stays the user's Wrath settings.
        WrathIPCWrapper.SetAutoRotationConfigState(id, AutoRotationConfigOption.BypassFATE, true);
        WrathIPCWrapper.SetAutoRotationConfigState(
            id,
            AutoRotationConfigOption.DPSRotationMode,
            manualTargeting ? DPSRotationMode.Manual : DPSRotationMode.Nearest);
        WrathIPCWrapper.SetAutoRotationConfigState(
            id,
            AutoRotationConfigOption.UnTargetAndDisableForPenalty,
            true);
        return WrathIPCWrapper.SetAutoRotationConfigState(id, AutoRotationConfigOption.AutoCleanse, true);
    }

    /// <summary>
    ///     Run an IPC call; on InvalidLease drop the Guid and retry once with a fresh registration.
    /// </summary>
    private bool InvokeWithLeaseRecovery(
        Func<Guid, SetResult> action,
        bool rearmAutoRotationAfterDrop = false)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            if (!Lease.HasValue)
            {
                return false;
            }

            SetResult result = action(Lease.Value);
            if (result != SetResult.InvalidLease)
            {
                // IGNORED = wrapper swallowed an exception; keep the lease.
                return IsAcceptable(result) || result == SetResult.IGNORED;
            }

            bool? desired = rotationOn;
            DropDeadLease();

            if (rearmAutoRotationAfterDrop && desired is { } on && Lease.HasValue)
            {
                SetResult restored = WrathIPCWrapper.SetAutoRotationState(Lease.Value, on);
                if (restored == SetResult.InvalidLease)
                {
                    continue;
                }
            }
        }

        return false;
    }

    private static HashSet<string> BuildOccultOptionsLeftOff(IReadOnlyCollection<string>? disabled)
    {
        HashSet<string> set = new(BuiltInOccultOptionsLeftOff, StringComparer.Ordinal);
        if (disabled != null)
        {
            set.UnionWith(disabled.Where(name => !string.IsNullOrWhiteSpace(name)));
        }

        return set;
    }

    private static bool IsOk(SetResult result) =>
        result is SetResult.Okay or SetResult.OkayWorking;

    private static bool IsAcceptable(SetResult result) =>
        result is SetResult.Okay or SetResult.OkayWorking or SetResult.Duplicate;

    /// <summary>
    ///     Turn a phantom job's pack off again. Best effort if Wrath is older than this IPC.
    /// </summary>
    private static void TryReleaseOccult(Guid leaseId, uint phantomJobId)
    {
        try
        {
            WrathIPCWrapper.SetOccultReadyForPhantomJob(leaseId, phantomJobId, enabled: false);
        }
        catch
        {
        }
    }

    private void TryLockOccultOptimal(Guid leaseId, uint phantomJobId)
    {
        try
        {
            string? parent = WrathIPCWrapper.GetOccultParentComboName(phantomJobId);
            if (string.IsNullOrEmpty(parent))
            {
                return;
            }

            List<string>? options = WrathIPCWrapper.GetOccultOptionNames(phantomJobId);

            // One call turns the parent and every option on. Older Wrath without the gate
            // reports a failure; fall back to setting each option individually below.
            bool bulk = IsOk(WrathIPCWrapper.SetOccultReadyForPhantomJob(leaseId, phantomJobId, enabled: true));

            WrathIPCWrapper.SetComboState(leaseId, parent, comboState: true, autoState: true);

            if (options == null)
            {
                return;
            }

            foreach (string option in options)
            {
                if (occultOptionsLeftOff.Contains(option))
                {
                    if (bulk)
                    {
                        WrathIPCWrapper.SetComboOptionState(leaseId, option, comboState: false);
                    }

                    continue;
                }

                if (!bulk)
                {
                    WrathIPCWrapper.SetComboOptionState(leaseId, option, comboState: true);
                }
            }
        }
        catch
        {
        }
    }
}
