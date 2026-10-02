namespace Ocelot.Ipc.RotationSolverReborn;

public interface IRotationSolverRebornIpc
{
    bool IsAvailable { get; }

    bool ChangeOperatingMode(RSRStateCommandType command);
}

public enum RSRStateCommandType : byte
{
    Off,

    Auto,

    TargetOnly,

    Manual,

    AutoDuty,

    Henched,

    PvP,
}
