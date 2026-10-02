namespace Ocelot.Chain;

public delegate Task<ChainResult> ChainMiddlewareDelegate();

public interface IChainMiddleware
{
    Task<ChainResult> InvokeAsync(IChainContext context, ChainMiddlewareDelegate next);
}

public delegate Task<StepResult> StepMiddlewareDelegate();

public interface IStepMiddleware
{
    Task<StepResult> InvokeAsync(IChainContext context, IStep step, StepMiddlewareDelegate next);
}
