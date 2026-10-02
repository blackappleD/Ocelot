using Microsoft.Extensions.DependencyInjection;
using Ocelot.Services.Logger;

namespace Ocelot.Lifecycle.Hosts;

public class StopHost(IServiceProvider services, ILogger<StopHost> logger) : BaseEventHost(logger), IOrderedHook
{
    private readonly Lazy<IOnStop[]> stopHooks = new(() =>
        services.GetServices<IOnStop>().OrderBy(h => h.Order).ToArray());

    public int Order
    {
        get => int.MinValue;
    }

    public override int Count
    {
        get => stopHooks.IsValueCreated ? stopHooks.Value.Length : 0;
    }

    public override void Start()
    {
    }

    public override void Stop()
    {
        SafeEach(stopHooks.Value, h => h.OnStop());
    }
}
