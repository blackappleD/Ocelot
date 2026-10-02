using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using DalamudPlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;

namespace Ocelot.Services.PlayerState;

public class Player(
    IPlayerState state,
    IObjectTable objects,
    ICondition condition,
    IDataManager data
) : IPlayer
{
    private const float MeleeRange = 3.5f;

    private const float RangedRange = 25f;
    
    private static readonly HashSet<uint> MeleeJobIds =
    [
        1, 2, 3, 4, // GLA, PUG, MRD, LNC
        19, 20, 21, 22, // PLD, MNK, WAR, DRG
        29, 30, 32, 34, // ROG, NIN, DRK, SAM
        37, 38, 39, 40, 41, // GNB, DNC, RPR, SGE, VPR
    ];

    private static readonly HashSet<uint> MeleeDpsJobIds =
    [
        2, 4, 20, 22, 29, 30, 34, 39, 41,
    ];

    private bool IsAvailable => PlayerCharacter != null;

    public IPlayerState State { get; } = state;

    public Vector3 Position => PlayerCharacter?.Position ?? Vector3.Zero;

    public ICondition Conditions { get; } = condition;

    public IPlayerCharacter? PlayerCharacter => objects.LocalPlayer;

    public int GetLevel() => PlayerCharacter?.Level ?? 0;

    public ClassJob? GetClassJob()
    {
        // Dalamud PlayerState tracks the real ClassJob; LocalPlayer can show phantom jobs in OC.
        if (State.IsLoaded && State.ClassJob.RowId != 0)
        {
            return State.ClassJob.Value;
        }

        unsafe
        {
            var playerState = DalamudPlayerState.Instance();
            if (playerState != null)
            {
                byte jobId = playerState->CurrentClassJobId;
                if (jobId != 0
                    && data.GameData.GetExcelSheet<ClassJob>()?.TryGetRow(jobId, out ClassJob job) == true)
                {
                    return job;
                }
            }
        }

        if (PlayerCharacter?.ClassJob is { RowId: not 0 } local
            && local.Value.Role is >= 1 and <= 5)
        {
            return local.Value;
        }

        return null;
    }

    public unsafe int GetLevel(ClassJob job)
    {
        var ps = DalamudPlayerState.Instance();
        return ps == null ? 0 : ps->ClassJobLevels[job.ExpArrayIndex];
    }

    public Vector3 GetPosition() => IsAvailable ? PlayerCharacter!.Position : Vector3.Zero;

    public bool IsMounting() =>
        Conditions[ConditionFlag.Mounting] || Conditions[ConditionFlag.Mounting71];

    public bool IsMounted() => Conditions[ConditionFlag.Mounted];

    public bool IsCasting() =>
        Conditions[ConditionFlag.Casting] || Conditions[ConditionFlag.Casting87];

    public bool IsBetweenAreas() =>
        Conditions[ConditionFlag.BetweenAreas] || Conditions[ConditionFlag.BetweenAreas51];

    public bool IsInteracting() =>
        Conditions.Any(
            ConditionFlag.OccupiedInEvent,
            ConditionFlag.OccupiedInQuestEvent,
            ConditionFlag.OccupiedInCutSceneEvent);

    public float GetAttackRange() => IsMelee() ? MeleeRange : RangedRange;

    public bool IsMelee()
    {
        var job = GetClassJob();
        if (job == null)
        {
            return false;
        }

        return job.Value.Role is 1 or 2 || MeleeJobIds.Contains(job.Value.RowId);
    }

    public bool IsTank()
    {
        var job = GetClassJob();
        return job?.Role == 1 || job?.RowId is 1 or 3 or 19 or 21 or 32 or 37;
    }

    public bool IsMeleeDps()
    {
        var job = GetClassJob();
        if (job == null)
        {
            return false;
        }

        return job.Value.Role == 2 || MeleeDpsJobIds.Contains(job.Value.RowId);
    }

    public bool IsHealer() => GetClassJob()?.Role == 4;

    public bool IsCaster() =>
        GetClassJob()?.RowId is 7 or 25 or 26 or 27 or 35 or 42;

    public bool CanFly()
    {
        if (!IsAvailable)
        {
            return false;
        }

        unsafe
        {
            var ps = DalamudPlayerState.Instance();
            return ps != null && ps->CanFly;
        }
    }
}
