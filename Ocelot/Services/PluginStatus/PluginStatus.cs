using Dalamud.Plugin;

namespace Ocelot.Services.PluginStatus;

public interface IPluginStatus
{
    bool IsLoaded(string internalName);

    bool IsInstalled(string internalName);
}

public class PluginStatus(IDalamudPluginInterface plugin) : IPluginStatus
{
    public bool IsLoaded(string internalName)
    {
        // InstalledPlugins can list unloaded copies of the same InternalName; require IsLoaded.
        return plugin.InstalledPlugins.Any(p => p.InternalName == internalName && p.IsLoaded);
    }

    public bool IsInstalled(string internalName) =>
        plugin.InstalledPlugins.Any(p => p.InternalName == internalName);
}
