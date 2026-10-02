using System.Numerics;
using Lumina.Excel.Sheets;

namespace Ocelot.Services.Pathfinding;

public readonly record struct PathfinderConfig(Func<Vector3> to)
{
    public PathfinderConfig(Vector3 to)
        : this(() => to)
    {
    }

    public Func<Vector3> To { get; } = to;

    public Vector3? From { get; init; }

    public float DistanceThreshold { get; init; } = 0f;

    public bool AllowFlying { get; init; } = false;

    public TerritoryType? TerritoryType { get; init; }

    public bool ShouldSnapToFloor { get; init; } = false;

    public float FloorSnapExtents { get; init; } = 5f;

    public System.Action? WhileMoving { get; init; }

    public static PathfinderConfig Fly(Vector3 to)
    {
        return new PathfinderConfig(to)
        {
            AllowFlying = true,
        };
    }
}
