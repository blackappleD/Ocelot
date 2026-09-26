using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Lifecycle;
using Ocelot.Services;
using Ocelot.Services.Translation;
using System.Threading;
using System.Threading.Tasks;

namespace Ocelot;

public abstract class OcelotPlugin : IAsyncDalamudPlugin
{
    private readonly IDalamudPluginInterface plugin;
    private ServiceProvider? services;
    private EventManager? host;

    public abstract string Name { get; }

    /// <summary>
    ///     Root slash command (kebab-cased). Defaults to the Dalamud InternalName; override when a
    ///     renamed build (e.g. a fork with a suffixed InternalName) should keep the original command.
    /// </summary>
    public virtual string CommandName
    {
        get => plugin.InternalName;
    }

    protected OcelotPlugin(IDalamudPluginInterface plugin, IPluginLog logger)
    {
        this.plugin = plugin;
    }

    protected abstract void Bootstrap(IServiceCollection collection);

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Registry.RegisterAssemblies(typeof(OcelotPlugin).Assembly, GetType().Assembly);

        var collection = new OcelotServiceCollection();

        collection.AddSingleton(plugin);
        collection.AddSingleton(this);

        collection.AddSingleton(new TranslatorContextResolverOptions(GetType()));

        collection.LoadDalamudServices(plugin);
        collection.LoadOcelotCore();
        collection.AutoDiscover();

        Bootstrap(collection);

        services = collection.Build();
        host = services.GetRequiredService<EventManager>();
        host.Start();

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        host?.Stop();
        host = null;

        services?.Dispose();
        services = null;

        return ValueTask.CompletedTask;
    }
}
