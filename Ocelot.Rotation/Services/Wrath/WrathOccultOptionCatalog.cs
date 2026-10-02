using Dalamud.Plugin;
using WrathCombo.API;

namespace Ocelot.Rotation.Services.Wrath;

public interface IWrathOccultOptionCatalog
{
    IReadOnlyList<string>? GetOptionNames(uint phantomJobId);

    IReadOnlySet<string> BuiltInOptionsLeftOff { get; }
}

public sealed class WrathOccultOptionCatalog(IDalamudPluginInterface pluginInterface) : IWrathOccultOptionCatalog
{
    public IReadOnlySet<string> BuiltInOptionsLeftOff => WrathJobRotation.BuiltInOccultOptionsLeftOff;

    public IReadOnlyList<string>? GetOptionNames(uint phantomJobId)
    {
        try
        {
            WrathIPCWrapper.Init(pluginInterface, WrathIPCWrapper.ErrorType.All);
            return WrathIPCWrapper.GetOccultOptionNames(phantomJobId);
        }
        catch
        {
            return null;
        }
    }
}
