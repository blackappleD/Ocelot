namespace Ocelot.Chain.Steps;

public class ActionStep(Func<IChainContext, ValueTask<StepResult>> action, string name) : IStep
{
    public Task<StepResult> ExecuteAsync(IChainContext context)
    {
        return action(context).AsTask();
    }

    public override string ToString()
    {
        return name;
    }
}

internal sealed class ConditionalActionStep(
    Func<IChainContext, ValueTask<bool>> condition,
    Func<IChainContext, ValueTask<StepResult>> thenAction,
    Func<IChainContext, ValueTask<StepResult>>? elseAction = null,
    string? name = null)
    : IStep
{
    private readonly Func<IChainContext, ValueTask<bool>> condition = condition ?? throw new ArgumentNullException(nameof(condition));
    private readonly Func<IChainContext, ValueTask<StepResult>> thenAction = thenAction ?? throw new ArgumentNullException(nameof(thenAction));
    private readonly Func<IChainContext, ValueTask<StepResult>> elseAction = elseAction ?? (_ => new ValueTask<StepResult>(StepResult.Success()));

    public async Task<StepResult> ExecuteAsync(IChainContext context)
    {
        var runThen = await condition(context);
        return runThen ? await thenAction(context) : await elseAction(context);
    }

    public override string ToString()
    {
        return name ?? nameof(ConditionalActionStep);
    }
}

public class ChainStep(IChain chain) : IStep
{
    public async Task<StepResult> ExecuteAsync(IChainContext context)
    {
        var result = await chain.ExecuteAsync(context);

        if (result.IsCanceled || context.CancellationToken.IsCancellationRequested)
        {
            return StepResult.Canceled();
        }

        return result.IsSuccess
            ? StepResult.Success()
            : StepResult.Failure(result.ErrorMessage ?? "Unknown error");
    }
}

public class WaitStep(TimeSpan delay, string? name = null) : IStep
{
    private readonly TimeSpan delay = delay < TimeSpan.Zero ? TimeSpan.Zero : delay;

    public WaitStep(int milliseconds, string? name = null)
        : this(TimeSpan.FromMilliseconds(milliseconds), name)
    {
    }

    public async Task<StepResult> ExecuteAsync(IChainContext context)
    {
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, context.CancellationToken);
        }

        return StepResult.Success();
    }

    public override string ToString()
    {
        return name ?? nameof(WaitStep);
    }
}
