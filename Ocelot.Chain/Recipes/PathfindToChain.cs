using System.Numerics;
using Dalamud.Plugin.Services;
using Ocelot.Chain.Extensions;
using Ocelot.Chain.Middleware.Chain;
using Ocelot.Chain.Middleware.Step;
using Ocelot.Chain.Steps;
using Ocelot.Extensions;
using Ocelot.Ipc.VNavmesh;
using Ocelot.Services.Logger;
using Ocelot.Services.Pathfinding;

namespace Ocelot.Chain.Recipes;

public class
    PathfindToChain(
    IChainFactory chains,
    IPathfinder pathfinder,
    IVNavmeshIpc vnav,
    IObjectTable objects,
    ILogger<PathfindToChain> logger
) : ChainRecipe<PathfinderConfig>(chains)
{
    private static readonly TimeSpan NavmeshReadyTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MovementStartTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MovementCompleteTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StuckTimeout = TimeSpan.FromSeconds(20);

    public override string Name { get; } = "Pathfind to Chain";

    protected override IChain Compose(IChain chain, PathfinderConfig pathfinderConfig)
    {
        DateTime lastProgressAt = DateTime.UtcNow;
        Vector3 lastProgressPos = Vector3.NaN;

        return chain
            .UseMiddleware<LogChainMiddleware>()
            .UseMiddleware(new RetryChainMiddleware(logger)
            {
                DelayMs = 500,
                MaxAttempts = 2,
            })
            .UseStepMiddleware<RunOnMainThreadMiddleware>()
            .UseStepMiddleware<LogStepMiddleware>()
            .Then(new WaitUntilStep(_ => new ValueTask<bool>(vnav.IsNavmeshReady()), NavmeshReadyTimeout, name: "Wait for navmesh"))
            .Then(_ =>
            {
                if (IsAtDestination(pathfinderConfig))
                {
                    return new ValueTask<StepResult>(StepResult.Break());
                }

                return new ValueTask<StepResult>(StepResult.Success());
            }, "Distance Check")
            .Then(_ =>
            {
                // Path.Stop does not cancel SimpleMove's pending pathfind task.
                pathfinder.Stop();
                vnav.Stop();
                return StepResult.Success();
            }, "Stop prior movement")
            .Then(new WaitUntilStep(
                _ =>
                {
                    if (vnav.IsPathfinding())
                    {
                        pathfinder.Stop();
                        vnav.Stop();
                        return new ValueTask<bool>(false);
                    }

                    return new ValueTask<bool>(true);
                },
                MovementStartTimeout,
                name: "Wait for pathfind slot"))
            .Then(_ =>
            {
                pathfinder.Stop();
                vnav.Stop();
                pathfinder.PathfindAndMoveTo(pathfinderConfig);
                pathfinderConfig.WhileMoving?.Invoke();
                lastProgressAt = DateTime.UtcNow;
                lastProgressPos = PlayerPosition();
                return StepResult.Success();
            }, "Start Pathfinder")
            .Then(new WaitUntilStep(_ => new ValueTask<bool>(IsAtDestination(pathfinderConfig) || pathfinder.GetState() != PathfindingState.Idle),
                MovementStartTimeout,
                name: "Wait for movement start"))
            .Then(new WaitUntilStep(_ =>
                {
                    if (IsAtDestination(pathfinderConfig))
                    {
                        return new ValueTask<bool>(true);
                    }

                    if (HasStoppedMoving())
                    {
                        return new ValueTask<bool>(true);
                    }

                    Vector3 pos = PlayerPosition();
                    pathfinderConfig.WhileMoving?.Invoke();
                    if (!float.IsNaN(pos.X))
                    {
                        if (float.IsNaN(lastProgressPos.X) || pos.Distance2D(lastProgressPos) > 1f)
                        {
                            lastProgressPos = pos;
                            lastProgressAt = DateTime.UtcNow;
                        }
                        else if (DateTime.UtcNow - lastProgressAt >= StuckTimeout)
                        {
                            logger.Warning("Pathfind appears stuck (no movement for {Seconds}s). Dist={Distance:F2}",
                                StuckTimeout.TotalSeconds, DistanceToDestination(pathfinderConfig));
                            return new ValueTask<bool>(true);
                        }
                    }

                    return new ValueTask<bool>(false);
                },
                MovementCompleteTimeout,
                name: "Wait for movement complete"))
            .Then(_ =>
            {
                if (!IsAtDestination(pathfinderConfig))
                {
                    // Stop() always reports Idle, so check first or a shortfall looks like a cancel.
                    bool stoppedEarly = HasStoppedMoving();
                    pathfinder.Stop();
                    vnav.Stop();

                    if (stoppedEarly)
                    {
                        logger.Debug(
                            "Pathfind stopped before destination (Distance={Distance:F2}) — treating as cancel",
                            DistanceToDestination(pathfinderConfig));
                        return new ValueTask<StepResult>(StepResult.Canceled());
                    }

                    logger.Warning(
                        "Pathfind did not reach destination. Distance={Distance:F2}, State={State}, VnavRunning={Running}, VnavPathfinding={Pathfinding}, NavmeshReady={NavmeshReady}",
                        DistanceToDestination(pathfinderConfig),
                        pathfinder.GetState(),
                        vnav.IsRunning(),
                        vnav.IsPathfinding(),
                        vnav.IsNavmeshReady());

                    return new ValueTask<StepResult>(StepResult.Failure("Did not reach destination"));
                }

                return new ValueTask<StepResult>(StepResult.Success());
            }, "Destination Check");
    }

    private bool HasStoppedMoving() =>
        pathfinder.GetState() == PathfindingState.Idle && !vnav.IsRunning() && !vnav.IsPathfinding();

    private Vector3 PlayerPosition() => objects.LocalPlayer?.Position ?? Vector3.NaN;

    private float ArrivalThreshold(PathfinderConfig config)
    {
        return config.DistanceThreshold > 0f ? config.DistanceThreshold : 2f;
    }

    private Vector3 ResolvedDestination(PathfinderConfig config)
    {
        Vector3 destination = config.To();
        if (config.ShouldSnapToFloor)
        {
            destination = pathfinder.SnapToMesh(destination, config.FloorSnapExtents);
        }

        return destination;
    }

    private float DistanceToDestination(PathfinderConfig config)
    {
        Vector3 destination = ResolvedDestination(config);
        Vector3 player = PlayerPosition();
        if (float.IsNaN(player.X))
        {
            return float.MaxValue;
        }

        if (!config.AllowFlying)
        {
            return player.Distance2D(destination);
        }

        return Vector3.Distance(player, destination);
    }

    private bool IsAtDestination(PathfinderConfig config)
    {
        return DistanceToDestination(config) <= ArrivalThreshold(config);
    }
}
