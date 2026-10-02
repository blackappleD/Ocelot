using Ocelot.Chain.Steps;

namespace Ocelot.Chain.Extensions;

public static class ChainExWait
{
    public static IChain Wait(this IChain chain, int waitMs, string? name = null)
    {
        return chain.Then(new WaitStep(waitMs, name ?? $"Wait {waitMs}ms"));
    }

    public static IChain Wait(this IChain chain, TimeSpan time, string? name = null)
    {
        return chain.Then(new WaitStep(time, name ?? $"Wait {time}"));
    }

    public static IChain WaitUntil(this IChain chain, DateTimeOffset when, string? name = null)
    {
        var now = DateTimeOffset.UtcNow;
        var until = when.ToUniversalTime();
        var delay = until - now;

        return chain.Wait(delay, name ?? $"Wait until {when:O}");
    }

    public static IChain WaitUntil(
        this IChain chain,
        Func<IChainContext, ValueTask<bool>> predicate,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        string? name = null
    )
    {
        return chain.Then(new WaitUntilStep(predicate, timeout, pollInterval, name));
    }
}
