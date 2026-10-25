using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Chain.Thread;

public static class ChainContextThreadExtensions
{
    private const string StartedOnMainThreadKey = "Ocelot.StartedOnMainThread";

    internal static void RecordStartThread(this IChainContext context)
    {
        if (!context.Data.ContainsKey(StartedOnMainThreadKey))
        {
            context.SetData(StartedOnMainThreadKey, context.ServiceProvider.GetService<IMainThread>()?.IsMainThread ?? false);
        }
    }

    // Task.Delay resumes on the thread pool; a run that started on the framework thread must keep game calls there.
    public static Task<T> RunOnStartThread<T>(this IChainContext context, Func<Task<T>> func)
    {
        if (context.GetData<bool>(StartedOnMainThreadKey)
            && context.ServiceProvider.GetService<IMainThread>() is { IsMainThread: false } mainThread)
        {
            return mainThread.InvokeAsync(func, context.CancellationToken);
        }

        return func();
    }
}
