using System.Numerics;

namespace Ocelot.Services.Pathfinding;

public interface IPathfinder
{
    PathfindingState GetState();

    void PathfindAndMoveTo(PathfinderConfig config);

    Task<Path> Pathfind(PathfinderConfig config);

    void FollowPath(Path path);

    Vector3 SnapToMesh(Vector3 point, float extent);

    void Stop();
}

public enum PathfindingState
{
    Idle,

    Pathfinding,

    Moving,
}
