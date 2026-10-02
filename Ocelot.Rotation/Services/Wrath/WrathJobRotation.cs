using Dalamud.Plugin;
using WrathCombo.API;
using WrathCombo.API.Enum;

namespace Ocelot.Rotation.Services.Wrath;

public sealed class WrathJobRotation(
    IDalamudPluginInterface pluginInterface,
    OcelotPlugin plugin) : IJobRotationBackend, IDisposable
{
    public static readonly IReadOnlySet<string> BuiltInOccultOptionsLeftOff = new HashSet<string>(StringComparer.Ordinal)
    {
        "Phantom_Chemist_OccultElixir",
        // Wrath has no useful Phantom BLM gate for this yet — bulk-enable spam-casts Toad.
        "Phantom_BlackMage_OccultToad",
    };

    private readonly Lock gate = new();

    private Lazy<Guid?> lease = NewLease(pluginInterface, plugin);

    private bool farmingDefaultsApplied;

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
