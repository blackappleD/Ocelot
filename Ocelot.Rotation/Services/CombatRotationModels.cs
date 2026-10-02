namespace Ocelot.Rotation.Services;

public enum CombatActivity
{
    Fate,
    CriticalEncounter,
    MobFarm,
}

public enum JobRotationBackendKind
{
    None = 0,
    Wrath = 1,
    RotationSolverReborn = 2,
    BossMod = 3,
    BossModReborn = 4,
}

public enum CombatAiKind
{
    None = 0,
    MiscAi = 1,
}

public enum BossModPresetKind
{
    MiscAi,
    FullAr,
}

public readonly record struct BossModMovementSettings(
    string RangeOption,
    string ForbiddenZoneCushion,
    string DelayMovement,
    string SeparateDodgeDelay,
    string DodgeDelayMovement)
{
    public static BossModMovementSettings Default { get; } = new("OnHitbox", "None", "None", "Disabled", "None");
}

public static class JobRotationBackendKeys
{
    public const string Wrath = "WrathCombo";

    public const string RotationSolverReborn = "RotationSolver";

    public const string BossMod = "BossMod";

    public const string BossModReborn = "BossModReborn";
}

public readonly record struct JobRotationSessionOptions(
    bool ManualTargeting = true,
    IReadOnlyCollection<string>? DisabledOccultOptions = null);

public readonly record struct CombatRotationRecipe(
    JobRotationBackendKind Job,
    CombatAiKind CombatAi,
    bool ManualTargeting = true,
    IReadOnlyCollection<string>? DisabledOccultOptions = null
)
{
    public static CombatRotationRecipe None { get; } = new(JobRotationBackendKind.None, CombatAiKind.None);

    public bool IsActive => Job != JobRotationBackendKind.None || CombatAi != CombatAiKind.None;
}

public sealed class CombatAiPresetNaming
{
    public string FateMiscAi { get; init; } = "BOCCHI AI FATE";

    public string CeMiscAi { get; init; } = "BOCCHI AI CE";

    public string MobFarmMiscAi { get; init; } = "BOCCHI AI MOB";

    public string FateFullAr { get; init; } = "BOCCHI AR FATE";

    public string CeFullAr { get; init; } = "BOCCHI AR CE";

    public string MobFarmFullAr { get; init; } = "BOCCHI AR MOB";

    public string PresetNameFor(CombatActivity activity, BossModPresetKind kind)
    {
        return (kind, activity) switch
        {
            (BossModPresetKind.FullAr, CombatActivity.Fate) => FateFullAr,
            (BossModPresetKind.FullAr, CombatActivity.CriticalEncounter) => CeFullAr,
            (BossModPresetKind.FullAr, _) => MobFarmFullAr,
            (_, CombatActivity.Fate) => FateMiscAi,
            (_, CombatActivity.CriticalEncounter) => CeMiscAi,
            _ => MobFarmMiscAi,
        };
    }
}
