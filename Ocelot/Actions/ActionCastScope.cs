using System.Threading;

namespace Ocelot.Actions;

public static class ActionCastScope
{
    private static readonly AsyncLocal<int> Depth = new();

    public static bool IsSuppressingPathfindCancel => Depth.Value > 0;

    public static IDisposable SuppressPathfindCancel() => new Scope();

    private sealed class Scope : IDisposable
    {
        public Scope() => Depth.Value++;

        public void Dispose()
        {
            if (Depth.Value > 0)
            {
                Depth.Value--;
            }
        }
    }
}
