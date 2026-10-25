using Ocelot.Chain.Thread;

namespace Ocelot.Chain.Middleware.Step;

public class RetryStepMiddleware : IStepMiddleware
{
    public int DelayMs { get; init; } = 0;

    public int MaxAttempts { get; init; } = int.MaxValue;

    public Func<Exception, bool>? ShouldRetryOnException { get; init; }

    public Func<StepResult, bool>? ShouldRetryOnResult { get; init; }

    public async Task<StepResult> InvokeAsync(IChainContext context, IStep step, StepMiddlewareDelegate next)
    {
        var attempt = 0;

        while (true)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            attempt++;

            try
            {
                var lastResult = await context.RunOnStartThread(() => next());

                if (lastResult.IsSuccess)
                {
                    return lastResult;
                }

                var shouldRetryResult = ShouldRetryOnResult?.Invoke(lastResult) ?? true;
                if (!shouldRetryResult || attempt >= MaxAttempts)
                {
                    return lastResult;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var shouldRetryException = ShouldRetryOnException?.Invoke(ex) ?? true;
                if (!shouldRetryException || attempt >= MaxAttempts)
                {
                    throw;
                }
            }

            var delay = TimeSpan.FromMilliseconds(DelayMs);
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, context.CancellationToken);
            }
        }
    }
}
