using Ocelot.Ipc.RotationSolverReborn;

namespace Ocelot.Rotation.Services.RotationSolverReborn;

public sealed class RsrJobRotation(IRotationSolverRebornIpc ipc) : IJobRotationBackend
{
    private static readonly TimeSpan FailedRetryInterval = TimeSpan.FromSeconds(1);

    public JobRotationBackendKind Kind => JobRotationBackendKind.RotationSolverReborn;

    private RSRStateCommandType desired = RSRStateCommandType.Off;

    private RSRStateCommandType? applied;

    private DateTimeOffset nextAttemptUtc = DateTimeOffset.MinValue;

    public void Prepare(JobRotationSessionOptions options)
    {
        _ = options;
        Disable();
    }

    public void Enable(CombatActivity activity)
    {
        _ = activity;
        // After Off (CE death) always re-issue Henched; RSR can no-op it while unconscious.
        if (desired != RSRStateCommandType.Henched || applied != RSRStateCommandType.Henched)
        {
            applied = null;
        }

        desired = RSRStateCommandType.Henched;
        Apply();
    }

    public void Disable()
    {
        desired = RSRStateCommandType.Off;
        Apply();
    }

    public void Refresh() => Apply();

    public void ClearAppliedCache() => applied = null;

    public void Teardown()
    {
        Disable();
        applied = null;
        nextAttemptUtc = DateTimeOffset.MinValue;
    }

    private void Apply()
    {
        if (applied == desired)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (now < nextAttemptUtc)
        {
            return;
        }

        bool ok = ipc.ChangeOperatingMode(desired);
        if (!ok && desired != RSRStateCommandType.Off)
        {
            nextAttemptUtc = now + FailedRetryInterval;
            return;
        }

        // Off is one-shot even if IPC throws (Wrath lease-cancel). Retrying reprints LeaseCancelled.
        applied = desired;
        nextAttemptUtc = DateTimeOffset.MinValue;
    }
}
