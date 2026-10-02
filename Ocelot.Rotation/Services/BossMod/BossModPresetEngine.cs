using System.Text;
using Ocelot.Ipc.BossMod;
using Ocelot.Services.PlayerState;

namespace Ocelot.Rotation.Services.BossMod;

public sealed class BossModPresetEngine(IBossModIpc ipc, IPlayer player, CombatAiPresetNaming naming)
{
    private const string LegacyAiPresetName = "BOCCHI AI";

    private const string LegacySingleTargetPresetName = "Ocelot Single Target";

    private static readonly string[] XanRoleAiModules =
    [
        "BossMod.Autorotation.xan.MeleeAI",
        "BossMod.Autorotation.xan.RangedAI",
        "BossMod.Autorotation.xan.TankAI",
        "BossMod.Autorotation.xan.HealerAI",
        "BossMod.Autorotation.xan.Caster",
        "BossMod.Autorotation.xan.PhantomAI",
    ];

    private static readonly string[] XanStandardJobModules =
    [
        "BossMod.Autorotation.xan.DRG",
        "BossMod.Autorotation.xan.MNK",
        "BossMod.Autorotation.xan.NIN",
        "BossMod.Autorotation.xan.RPR",
        "BossMod.Autorotation.xan.SAM",
        "BossMod.Autorotation.xan.VPR",
        "BossMod.Autorotation.xan.DRK",
        "BossMod.Autorotation.xan.GNB",
        "BossMod.Autorotation.xan.PLD",
        "BossMod.Autorotation.xan.BRD",
        "BossMod.Autorotation.xan.DNC",
        "BossMod.Autorotation.xan.MCH",
        "BossMod.Autorotation.xan.AST",
        "BossMod.Autorotation.xan.SCH",
        "BossMod.Autorotation.xan.SGE",
        "BossMod.Autorotation.xan.WHM",
        "BossMod.Autorotation.xan.BLM",
        "BossMod.Autorotation.xan.PCT",
        "BossMod.Autorotation.xan.RDM",
        "BossMod.Autorotation.xan.SMN",
        "BossMod.Autorotation.VeynWAR",
    ];

    private bool wantOwned;

    private bool wantActive;

    private bool presetsReady;

    private CombatActivity activeActivity = CombatActivity.Fate;

    private CombatActivity? armedActivity;

    private BossModPresetKind presetKind = BossModPresetKind.MiscAi;

    private bool? bakedAsMelee;

    private uint? bakedJobId;

    private BossModMovementSettings? bakedMovement;

    public bool OverwriteExisting { get; set; }

    public BossModMovementSettings Movement { get; set; } = BossModMovementSettings.Default;

    public bool IsAvailable => ipc.IsAvailable;

    public void EnsurePresets(BossModPresetKind kind)
    {
        wantOwned = true;
        if (presetKind != kind)
        {
            ClearBakeState();
        }

        presetKind = kind;
        DeleteLegacyPresets();
        presetsReady = WriteOwnedPresets(player.IsMelee(), OverwriteExisting);
    }

    public void Enable(CombatActivity activity)
    {
        wantOwned = true;

        bool alreadyArmed = wantActive
                            && armedActivity == activity
                            && presetsReady
                            && IsPresetActiveFor(activity);
        if (alreadyArmed)
        {
            return;
        }

        wantActive = true;
        activeActivity = activity;
        Refresh();
        if (!presetsReady || !ipc.IsAvailable)
        {
            return;
        }

        ActivateCurrent();
        armedActivity = activity;
    }

    public void Disable()
    {
        bool needsDeactivate = wantActive
                               || armedActivity != null
                               || (ipc.IsAvailable && IsAnyOwnedPresetActive());
        wantActive = false;
        armedActivity = null;
        ClearAppliedMovement();
        if (!needsDeactivate || !ipc.IsAvailable)
        {
            return;
        }

        DeactivateAllOwnedPresets();
    }

    public void Refresh()
    {
        if (!wantOwned)
        {
            return;
        }

        if (!ipc.IsAvailable)
        {
            presetsReady = false;
            return;
        }

        uint jobId = CurrentJobId();
        bool isMelee = player.IsMelee();
        string fate = FateName();
        string ce = CeName();
        string mob = MobFarmName();
        bool missing = ipc.Get(fate) == null || ipc.Get(ce) == null || ipc.Get(mob) == null;
        bool jobChanged = OverwriteExisting && bakedJobId is not null && bakedJobId.Value != jobId;
        bool roleChanged = OverwriteExisting && bakedAsMelee is not null && bakedAsMelee.Value != isMelee;
        bool movementChanged = OverwriteExisting
                               && bakedMovement is not null
                               && bakedMovement != Movement;

        if (!presetsReady || missing || jobChanged || roleChanged || movementChanged)
        {
            bool wasActive = wantActive || IsAnyOwnedPresetActive();
            armedActivity = null;
            bool overwrite = OverwriteExisting
                             && (jobChanged || roleChanged || movementChanged || !presetsReady);
            presetsReady = WriteOwnedPresets(isMelee, overwrite);
            if (presetsReady && wasActive)
            {
                wantActive = true;
                ActivateCurrent();
                armedActivity = activeActivity;
            }
        }

        if (wantActive && presetsReady)
        {
            ApplyMovement(naming.PresetNameFor(activeActivity, presetKind));
        }
    }

    public void Teardown()
    {
        wantOwned = false;
        wantActive = false;
        ClearBakeState();

        if (!ipc.IsAvailable)
        {
            return;
        }

        DeactivateAllOwnedPresets();
        ClearAppliedMovement();
        presetKind = BossModPresetKind.MiscAi;
    }

    public bool TryEnsureMiscAiPresets(out string? storedJson)
    {
        EnsurePresets(BossModPresetKind.MiscAi);
        storedJson = null;
        if (!presetsReady)
        {
            return false;
        }

        storedJson =
            $"=== {naming.FateMiscAi} ===\n{ipc.Get(naming.FateMiscAi)}\n\n"
            + $"=== {naming.CeMiscAi} ===\n{ipc.Get(naming.CeMiscAi)}\n\n"
            + $"=== {naming.MobFarmMiscAi} ===\n{ipc.Get(naming.MobFarmMiscAi)}";
        return true;
    }

    public bool TryForceRecreate(BossModPresetKind kind)
    {
        if (!ipc.IsAvailable)
        {
            return false;
        }

        presetKind = kind;
        armedActivity = null;
        ClearAppliedMovement();
        presetsReady = WriteOwnedPresets(player.IsMelee(), overwrite: true);
        if (presetsReady && wantActive)
        {
            ActivateCurrent();
            armedActivity = activeActivity;
        }

        return presetsReady;
    }

    private string FateName() => naming.PresetNameFor(CombatActivity.Fate, presetKind);

    private string CeName() => naming.PresetNameFor(CombatActivity.CriticalEncounter, presetKind);

    private string MobFarmName() => naming.PresetNameFor(CombatActivity.MobFarm, presetKind);

    private uint CurrentJobId() => player.GetClassJob()?.RowId ?? 0;

    private void ClearBakeState()
    {
        presetsReady = false;
        armedActivity = null;
        bakedAsMelee = null;
        bakedJobId = null;
        bakedMovement = null;
    }

    private void DeleteLegacyPresets()
    {
        DeletePresetIfPresent(LegacyAiPresetName);
        DeletePresetIfPresent(LegacySingleTargetPresetName);
    }

    private void DeletePresetIfPresent(string name)
    {
        if (!ipc.IsAvailable || ipc.Get(name) == null)
        {
            return;
        }

        ipc.Deactivate(name);
        ipc.Delete(name);
    }

    private void DeactivateAllOwnedPresets()
    {
        ipc.Deactivate(naming.FateMiscAi);
        ipc.Deactivate(naming.CeMiscAi);
        ipc.Deactivate(naming.MobFarmMiscAi);
        ipc.Deactivate(naming.FateFullAr);
        ipc.Deactivate(naming.CeFullAr);
        ipc.Deactivate(naming.MobFarmFullAr);
        ipc.Deactivate(LegacyAiPresetName);
    }

    private bool IsAnyOwnedPresetActive()
    {
        string? active = ipc.GetActive();
        return string.Equals(active, naming.FateMiscAi, StringComparison.Ordinal)
               || string.Equals(active, naming.CeMiscAi, StringComparison.Ordinal)
               || string.Equals(active, naming.MobFarmMiscAi, StringComparison.Ordinal)
               || string.Equals(active, naming.FateFullAr, StringComparison.Ordinal)
               || string.Equals(active, naming.CeFullAr, StringComparison.Ordinal)
               || string.Equals(active, naming.MobFarmFullAr, StringComparison.Ordinal)
               || string.Equals(active, LegacyAiPresetName, StringComparison.Ordinal);
    }

    private bool IsPresetActiveFor(CombatActivity activity)
    {
        if (!ipc.IsAvailable)
        {
            return false;
        }

        return string.Equals(
            ipc.GetActive(),
            naming.PresetNameFor(activity, presetKind),
            StringComparison.Ordinal);
    }

    private void ActivateCurrent()
    {
        string wanted = naming.PresetNameFor(activeActivity, presetKind);
        ipc.Deactivate(LegacyAiPresetName);
        if (presetKind == BossModPresetKind.FullAr)
        {
            ipc.Deactivate(naming.FateMiscAi);
            ipc.Deactivate(naming.CeMiscAi);
            ipc.Deactivate(naming.MobFarmMiscAi);
            DeactivateOwnedExcept(wanted, BossModPresetKind.FullAr);
        }
        else
        {
            ipc.Deactivate(naming.FateFullAr);
            ipc.Deactivate(naming.CeFullAr);
            ipc.Deactivate(naming.MobFarmFullAr);
            DeactivateOwnedExcept(wanted, BossModPresetKind.MiscAi);
        }

        ipc.Activate(wanted);
        ApplyMovement(wanted);
    }

    private void DeactivateOwnedExcept(string wanted, BossModPresetKind kind)
    {
        foreach (CombatActivity activity in new[]
                 {
                     CombatActivity.Fate,
                     CombatActivity.CriticalEncounter,
                     CombatActivity.MobFarm,
                 })
        {
            string name = naming.PresetNameFor(activity, kind);
            if (!string.Equals(name, wanted, StringComparison.Ordinal))
            {
                ipc.Deactivate(name);
            }
        }
    }

    private BossModMovementSettings appliedMovement;

    private string? appliedMovementPreset;

    private void ApplyMovement(string preset)
    {
        if (!ipc.IsAvailable || string.IsNullOrEmpty(preset))
        {
            return;
        }

        if (appliedMovementPreset == preset && appliedMovement == Movement)
        {
            return;
        }

        const string stayClose = "BossMod.Autorotation.MiscAI.StayCloseToTarget";
        const string normal = "BossMod.Autorotation.MiscAI.NormalMovement";
        ipc.AddTransientStrategy(preset, stayClose, "range", Movement.RangeOption);
        ipc.AddTransientStrategy(preset, normal, "ForbiddenZoneCushion", Movement.ForbiddenZoneCushion);
        ipc.AddTransientStrategy(preset, normal, "DelayMovement", Movement.DelayMovement);
        ipc.AddTransientStrategy(preset, normal, "SeparateDodgeDelay", Movement.SeparateDodgeDelay);
        ipc.AddTransientStrategy(preset, normal, "DodgeDelayMovement", Movement.DodgeDelayMovement);
        appliedMovementPreset = preset;
        appliedMovement = Movement;
    }

    private void ClearAppliedMovement()
    {
        appliedMovementPreset = null;
        appliedMovement = default;
    }

    private bool WriteOwnedPresets(bool isMelee, bool overwrite)
    {
        if (!ipc.IsAvailable)
        {
            return false;
        }

        DeleteLegacyPresets();
        if (overwrite)
        {
            ClearAppliedMovement();
        }

        bool fateOk = WritePreset(FateName(), BuildPresetJson(FateName(), CombatActivity.Fate), overwrite);
        bool ceOk = WritePreset(CeName(), BuildPresetJson(CeName(), CombatActivity.CriticalEncounter), overwrite);
        bool mobOk = WritePreset(MobFarmName(), BuildPresetJson(MobFarmName(), CombatActivity.MobFarm), overwrite);
        bool ok = fateOk && ceOk && mobOk;
        if (ok)
        {
            bakedAsMelee = isMelee;
            bakedJobId = CurrentJobId();
            bakedMovement = Movement;
        }
        else
        {
            ClearBakeState();
        }

        return ok;
    }

    private bool WritePreset(string name, string json, bool overwrite)
    {
        if (ipc.Get(name) != null)
        {
            if (!overwrite)
            {
                return true;
            }

            ipc.Deactivate(name);
            ipc.Delete(name);
        }

        ipc.Create(json, overwrite: true);
        return ipc.Get(name) != null;
    }

    private string BuildPresetJson(string name, CombatActivity activity)
    {
        string rangeOption = Movement.RangeOption;
        string cushion = Movement.ForbiddenZoneCushion;
        string delay = Movement.DelayMovement;
        string separateDodge = Movement.SeparateDodgeDelay;
        string dodgeDelay = Movement.DodgeDelayMovement;
        bool fateOnlyTargets = activity == CombatActivity.Fate;
        return presetKind == BossModPresetKind.FullAr
            ? BuildFullArPresetJson(name, fateOnlyTargets, rangeOption, cushion, delay, separateDodge, dodgeDelay)
            : BuildMiscAiPresetJson(name, fateOnlyTargets, rangeOption, cushion, delay, separateDodge, dodgeDelay);
    }

    private static string BuildMiscAiPresetJson(
        string name,
        bool fateOnlyTargets,
        string rangeOption,
        string cushion,
        string delay,
        string separateDodge,
        string dodgeDelay)
    {
        string fateOption = fateOnlyTargets ? "Enabled" : "Disabled";
        string everythingOption = fateOnlyTargets ? "Disabled" : "Enabled";

        return
            $$"""
            {
              "Name": "{{name}}",
              "Modules": {
                {{BuildMiscAiModulesJson(rangeOption, fateOption, everythingOption, cushion, delay, separateDodge, dodgeDelay)}}
              }
            }
            """;
    }

    private static string BuildFullArPresetJson(
        string name,
        bool fateOnlyTargets,
        string rangeOption,
        string cushion,
        string delay,
        string separateDodge,
        string dodgeDelay)
    {
        string fateOption = fateOnlyTargets ? "Enabled" : "Disabled";
        string everythingOption = fateOnlyTargets ? "Disabled" : "Enabled";

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"Name\": \"{name}\",");
        sb.AppendLine("  \"Modules\": {");
        sb.Append(BuildMiscAiModulesJson(rangeOption, fateOption, everythingOption, cushion, delay, separateDodge, dodgeDelay));
        sb.AppendLine(",");
        for (int i = 0; i < XanRoleAiModules.Length; i++)
        {
            string module = XanRoleAiModules[i];
            sb.Append($"    \"{module}\": {RoleAiStrategies(module)}");
            sb.AppendLine(",");
        }

        static string JobStrategies(string module) =>
            module.EndsWith("VeynWAR", StringComparison.Ordinal)
                ? """
                  [
                        { "Track": "AOE", "Option": "ForceAOE" },
                        { "Track": "Burst", "Option": "Spend" },
                        { "Track": "Potion", "Option": "Manual" },
                        { "Track": "FC", "Option": "Automatic" },
                        { "Track": "Infuriate", "Option": "ForceIfChargesCapping" },
                        { "Track": "IR", "Option": "Automatic" },
                        { "Track": "Upheaval", "Option": "Automatic" },
                        { "Track": "PR", "Option": "Automatic" },
                        { "Track": "Onslaught", "Option": "NoReserve" },
                        { "Track": "Tomahawk", "Option": "OpenerRanged" }
                      ]
                  """
                : """
                  [
                        { "Track": "AOE", "Option": "AOE" }
                      ]
                  """;

        static string RoleAiStrategies(string module) =>
            module.EndsWith("PhantomAI", StringComparison.Ordinal)
                ? """
                  [
                        { "Track": "Chemist", "Option": "InCombat" },
                        { "Track": "WHMRaise", "Option": "InCombat" }
                      ]
                  """
                : module.EndsWith("HealerAI", StringComparison.Ordinal)
                    ? """
                      [
                            { "Track": "Raise", "Option": "Swiftcast" },
                            { "Track": "RaiseTargets", "Option": "Everyone" },
                            { "Track": "Heal", "Option": "Enabled" },
                            { "Track": "Esuna2", "Option": "Enabled" },
                            { "Track": "Stay near party", "Option": "Enabled" },
                            { "Track": "OutOfCombat", "Option": "Enabled" }
                          ]
                      """
                    : "[]";

        for (int i = 0; i < XanStandardJobModules.Length; i++)
        {
            string module = XanStandardJobModules[i];
            sb.Append($"    \"{module}\": {JobStrategies(module)}");
            if (i < XanStandardJobModules.Length - 1)
            {
                sb.Append(',');
            }

            sb.AppendLine();
        }

        sb.AppendLine("  }");
        sb.Append('}');
        return sb.ToString();
    }

    private static string BuildMiscAiModulesJson(
        string rangeOption,
        string fateOption,
        string everythingOption,
        string cushion,
        string delay,
        string separateDodge,
        string dodgeDelay) =>
        $$"""
            "BossMod.Autorotation.MiscAI.AutoTarget": [
              { "Track": "General", "Option": "Aggressive" },
              { "Track": "Retarget", "Option": "Always" },
              { "Track": "Treasure", "Option": "Disabled" },
              { "Track": "FATE", "Option": "{{fateOption}}" },
              { "Track": "Everything", "Option": "{{everythingOption}}" }
            ],
            "BossMod.Autorotation.MiscAI.StayWithinLeylines": [
              { "Track": "Use Between The Lines", "Option": "Yes" },
              { "Track": "Use Retrace", "Option": "Yes" }
            ],
            "BossMod.Autorotation.MiscAI.GoToPositional": [],
            "BossMod.Autorotation.MiscAI.StayCloseToTarget": [
              { "Track": "range", "Option": "{{rangeOption}}" }
            ],
            "BossMod.Autorotation.MiscAI.NormalMovement": [
              { "Track": "Destination", "Option": "Pathfind" },
              { "Track": "ForbiddenZoneCushion", "Option": "{{cushion}}" },
              { "Track": "Range", "Option": "Any" },
              { "Track": "DelayMovement", "Option": "{{delay}}" },
              { "Track": "SeparateDodgeDelay", "Option": "{{separateDodge}}" },
              { "Track": "DodgeDelayMovement", "Option": "{{dodgeDelay}}" },
              { "Track": "Cast", "Option": "Leeway" }
            ]
        """;
}
