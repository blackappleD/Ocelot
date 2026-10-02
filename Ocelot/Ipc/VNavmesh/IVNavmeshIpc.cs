using System.Numerics;

namespace Ocelot.Ipc.VNavmesh;

public interface IVNavmeshIpc
{
    bool IsAvailable();

    bool IsNavmeshReady();

    bool IsPathfinding();

    bool IsRunning();

    Task<List<Vector3>> Pathfind(Vector3 start, Vector3 end, bool fly, CancellationToken? cancel = null);

    Task<List<Vector3>> Pathfind(Vector3 start, Vector3 end, bool fly, float range);

    void FollowPath(List<Vector3> path, bool fly);

    void PathfindAndMoveTo(Vector3 destination, bool shouldFly);

    void PathfindAndMoveCloseTo(Vector3 destination, bool shouldFly, float range);

    Vector3 FindPointOnFloor(Vector3 origin, float halfExtentXZ);

    Vector3 FindPointOnMesh(Vector3 origin, float halfExtentXZ, float halfExtentY);

    bool TryFindPointOnFloor(Vector3 origin, float halfExtentXZ, out Vector3 point);

    bool TryFindPointOnMesh(Vector3 origin, float halfExtentXZ, float halfExtentY, out Vector3 point);

    void Stop();
}
