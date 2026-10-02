using System.Runtime.CompilerServices;
using Dalamud.Plugin.Services;

namespace Ocelot.Services.Gate;

public sealed class GateService(IFramework framework) : IGateService
{
    private sealed class ScopeBucket
    {
        public readonly Dictionary<string, long> Elapsed = new();
    }

    private readonly ConditionalWeakTable<object, ScopeBucket> buckets = new();

    public bool Milliseconds(object owner, string scope, long interval)
    {
        if (interval <= 0)
        {
            return true;
        }

        var bucket = buckets.GetOrCreateValue(owner);

        bucket.Elapsed.TryGetValue(scope, out var elapsed);
        elapsed += framework.UpdateDelta.Milliseconds;

        if (elapsed >= interval)
        {
            bucket.Elapsed[scope] = elapsed - interval;
            return true;
        }

        bucket.Elapsed[scope] = elapsed;
        return false;
    }

    public bool UpdatesPerSecond(object owner, string scope, int ups)
    {
        return Milliseconds(owner, scope, ups <= 0 ? 0 : 1000 / ups);
    }

    public bool Seconds(object owner, string scope, int seconds)
    {
        return Milliseconds(owner, scope, (long)seconds * 1000);
    }

    public bool Minutes(object owner, string scope, int minutes)
    {
        return Seconds(owner, scope, minutes * 60
        );
    }

    public void Reset(object owner)
    {
        if (buckets.TryGetValue(owner, out var bucket))
        {
            bucket.Elapsed.Clear();
        }
    }

    public void Reset(object owner, string scope)
    {
        if (buckets.TryGetValue(owner, out var bucket))
        {
            bucket.Elapsed.Remove(scope);
        }
    }
}
