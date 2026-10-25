using Microsoft.Extensions.DependencyInjection;
using Ocelot.Chain.Steps;
using Ocelot.Chain.Thread;

namespace Ocelot.Chain;

public class Chain(string name, IServiceProvider services) : IChain
{
    public string Name { get; } = name;

    private readonly List<IStep> steps = [];

    private readonly List<IChainMiddleware> chainMiddleware = [];

    private readonly List<IStepMiddleware> stepMiddleware = [];

    public IChain Then(IStep step)
    {
        steps.Add(step);
        return this;
    }

    public IChain Then(IChain chain)
    {
        return Then(new ChainStep(chain));
    }

    public IChain Then<T>() where T : class
    {
        if (typeof(IStep).IsAssignableFrom(typeof(T)))
        {
            return Then((IStep)services.GetRequiredService<T>());
        }

        if (typeof(IChainRecipe).IsAssignableFrom(typeof(T)))
        {
            var recipe = (IChainRecipe)services.GetRequiredService<T>();
            return Then(recipe.Build());
        }

        return this;
    }

    public IChain Then<TRecipe, TArgs>(TArgs args) where TRecipe : class, IChainRecipe<TArgs>
    {
        var recipe = services.GetRequiredService<TRecipe>();
        return Then(recipe.Build(args));
    }

    public IChain UseMiddleware(IChainMiddleware middleware)
    {
        chainMiddleware.Add(middleware);
        return this;
    }

    public IChain UseMiddleware<TMiddleware>() where TMiddleware : class, IChainMiddleware
    {
        chainMiddleware.Add(services.GetRequiredService<TMiddleware>());
        return this;
    }


    public IChain UseStepMiddleware(IStepMiddleware middleware)
    {
        stepMiddleware.Add(middleware);
        return this;
    }

    public IChain UseStepMiddleware<TMiddleware>() where TMiddleware : class, IStepMiddleware
    {
        stepMiddleware.Add(services.GetRequiredService<TMiddleware>());
        return this;
    }

    public async Task<ChainResult> ExecuteAsync(IChainContext context)
    {
        try
        {
            var chainExecution = BuildChainMiddlewarePipeline(context);
            return await chainExecution();
        }
        catch (OperationCanceledException)
        {
            return ChainResult.Canceled();
        }
        catch (ObjectDisposedException)
        {
            return ChainResult.Canceled();
        }
        catch (Exception ex)
        {
            return ChainResult.Failure(ex);
        }
    }

    public async Task<ChainResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var context = new ChainContext(Name, services, cancellationToken);
        return await ExecuteAsync(context);
    }

    private ChainMiddlewareDelegate BuildChainMiddlewarePipeline(IChainContext context)
    {
        ChainMiddlewareDelegate pipeline = () => ExecuteStepsAsync(context);

        for (var i = chainMiddleware.Count - 1; i >= 0; i--)
        {
            var middleware = chainMiddleware[i];
            var next = pipeline;
            pipeline = () => middleware.InvokeAsync(context, next);
        }

        return pipeline;
    }

    private async Task<ChainResult> ExecuteStepsAsync(IChainContext context)
    {
        context.RecordStartThread();

        foreach (var step in steps)
        {
            try
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                StepMiddlewareDelegate pipeline = () => step.ExecuteAsync(context);

                var local = step.GetMiddleware().ToList();
                for (var i = local.Count - 1; i >= 0; i--)
                {
                    var middleware = local[i];
                    var next = pipeline;
                    pipeline = () => middleware.InvokeAsync(context, step, next);
                }

                for (var i = stepMiddleware.Count - 1; i >= 0; i--)
                {
                    var mw = stepMiddleware[i];
                    var next = pipeline;
                    pipeline = () => mw.InvokeAsync(context, step, next);
                }

                var result = await context.RunOnStartThread(() => pipeline());

                if (result.IsCanceled || context.CancellationToken.IsCancellationRequested)
                {
                    return ChainResult.Canceled();
                }

                switch (result.IsSuccess)
                {
                    case true when result.ShouldBreak:
                        return ChainResult.Success();
                    case false:
                        return ChainResult.Failure(result);
                }
            }
            catch (OperationCanceledException)
            {
                return ChainResult.Canceled();
            }
            catch (ObjectDisposedException)
            {
                return ChainResult.Canceled();
            }
            catch (Exception ex)
            {
                return ChainResult.Failure($"Step {step.GetType().Name} threw an exception: {ex.Message}");
            }
        }

        return ChainResult.Success();
    }
}
