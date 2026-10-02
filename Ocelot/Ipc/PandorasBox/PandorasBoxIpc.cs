using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace Ocelot.Ipc.PandorasBox;

public sealed class PandorasBoxIpc(IDalamudPluginInterface plugin) : IPandorasBoxIpc
{
    private readonly ICallGateSubscriber<string, bool?> getFeatureEnabledInternal =
        plugin.GetIpcSubscriber<string, bool?>("PandorasBox.GetFeatureEnabledInternal");

    private readonly ICallGateSubscriber<string, bool, object> setFeatureEnabledInternal =
        plugin.GetIpcSubscriber<string, bool, object>("PandorasBox.SetFeatureEnabledInternal");

    public bool IsAvailable
    {
        get
        {
            try
            {
                return getFeatureEnabledInternal.HasFunction && setFeatureEnabledInternal.HasFunction;
            }
            catch
            {
                return false;
            }
        }
    }

    public bool? GetFeatureEnabledInternal(string internalName)
    {
        try
        {
            return getFeatureEnabledInternal.InvokeFunc(internalName);
        }
        catch
        {
            return null;
        }
    }

    public void SetFeatureEnabledInternal(string internalName, bool enabled)
    {
        try
        {
            setFeatureEnabledInternal.InvokeAction(internalName, enabled);
        }
        catch
        {
        }
    }
}
