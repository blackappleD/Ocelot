using Ocelot.Pathfinding.Services.Navmesh;

namespace Ocelot.Pathfinding.Services;

public interface IPathfindingPriorityService
{
    IEnumerable<string> GetPriority();
}

public class PathfindingPriorityService : IPathfindingPriorityService
{
    public IEnumerable<string> GetPriority()
    {
        return
        [
            NavmeshPathfindingProvider.Key,
        ];
    }
}
