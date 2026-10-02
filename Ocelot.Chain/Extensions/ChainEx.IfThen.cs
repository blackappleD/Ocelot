using Ocelot.Chain.Steps;

namespace Ocelot.Chain.Extensions;

public static class ChainExIfThen
{
    public static IChain IfThen(
        this IChain chain,
        Func<IChainContext, ValueTask<bool>> condition,
        Func<IChainContext, ValueTask<StepResult>> thenAction,
        string? name = null)
    {
        return chain.Then(new ConditionalActionStep(condition, thenAction, name: name));
    }

    public static IChain IfThenElse(
        this IChain chain,
        Func<IChainContext, ValueTask<bool>> condition,
        Func<IChainContext, ValueTask<StepResult>> thenAction,
        Func<IChainContext, ValueTask<StepResult>> elseAction,
        string? name = null)
    {
        return chain.Then(new ConditionalActionStep(condition, thenAction, elseAction, name));
    }

    public static IChain IfThen(
        this IChain chain,
        Func<IChainContext, CancellationToken, ValueTask<bool>> condition,
        Func<IChainContext, ValueTask<StepResult>> thenAction,
        string? name = null)
    {
        return chain.Then(new ConditionalActionStep(
            ctx => condition(ctx, ctx.CancellationToken),
            thenAction,
            name: name));
    }

    public static IChain IfThenElse(
        this IChain chain,
        Func<IChainContext, CancellationToken, ValueTask<bool>> condition,
        Func<IChainContext, ValueTask<StepResult>> thenAction,
        Func<IChainContext, ValueTask<StepResult>> elseAction,
        string? name = null)
    {
        return chain.Then(new ConditionalActionStep(
            ctx => condition(ctx, ctx.CancellationToken),
            thenAction,
            elseAction,
            name));
    }

    public static IChain IfThen(
        this IChain chain,
        Func<IChainContext, bool> condition,
        Func<IChainContext, ValueTask<StepResult>> thenAction,
        string? name = null)
    {
        return chain.Then(new ConditionalActionStep(
            ctx => new ValueTask<bool>(condition(ctx)),
            thenAction,
            name: name));
    }

    public static IChain IfThen(
        this IChain chain,
        Func<bool> condition,
        Func<IChainContext, ValueTask<StepResult>> thenAction,
        string? name = null)
    {
        return chain.Then(new ConditionalActionStep(
            _ => new ValueTask<bool>(condition()),
            thenAction,
            name: name));
    }

    public static IChain IfThen(
        this IChain chain,
        bool condition,
        Func<IChainContext, ValueTask<StepResult>> thenAction,
        string? name = null)
    {
        return chain.Then(new ConditionalActionStep(
            _ => new ValueTask<bool>(condition),
            thenAction,
            name: name));
    }

    public static IChain IfThen(
        this IChain chain,
        Func<IChainContext, Task<bool>> condition,
        Func<IChainContext, ValueTask<StepResult>> thenAction,
        string? name = null)
    {
        return chain.Then(new ConditionalActionStep(
            async ctx => await condition(ctx),
            thenAction,
            name: name));
    }

    public static IChain IfThen(
        this IChain chain,
        Func<IChainContext, ValueTask<bool>> condition,
        Action<IChainContext> thenAction,
        string? name = null)
    {
        return chain.Then(new ConditionalActionStep(
            condition,
            ctx =>
            {
                thenAction(ctx);
                return new ValueTask<StepResult>(StepResult.Success());
            },
            name: name));
    }

    public static IChain IfThenElse(
        this IChain chain,
        Func<IChainContext, ValueTask<bool>> condition,
        Action<IChainContext> thenAction,
        Action<IChainContext> elseAction,
        string? name = null)
    {
        return chain.Then(new ConditionalActionStep(
            condition,
            ctx =>
            {
                thenAction(ctx);
                return new ValueTask<StepResult>(StepResult.Success());
            },
            ctx =>
            {
                elseAction(ctx);
                return new ValueTask<StepResult>(StepResult.Success());
            },
            name));
    }

    public static IChain IfThen(
        this IChain chain,
        Func<IChainContext, ValueTask<bool>> condition,
        Func<Task> thenAction,
        string? name = null)
    {
        return chain.Then(new ConditionalActionStep(
            condition,
            async _ =>
            {
                await thenAction();
                return StepResult.Success();
            },
            name: name));
    }
}
