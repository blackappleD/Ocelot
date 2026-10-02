using Dalamud.Configuration;
using Dalamud.Plugin;

namespace Ocelot.Config;

public interface IConfigSaver
{
    void Save();
}

public class ConfigSaver(IDalamudPluginInterface plugin, IPluginConfiguration config) : IConfigSaver
{
    public void Save()
    {
        plugin.SavePluginConfig(config);
    }
}
