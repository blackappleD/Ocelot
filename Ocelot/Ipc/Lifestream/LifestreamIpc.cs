using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace Ocelot.Ipc.Lifestream;

public class LifestreamIpc(IDalamudPluginInterface plugin) : ILifestreamIpc
{
    private readonly ICallGateSubscriber<bool> isBusy = plugin.GetIpcSubscriber<bool>("Lifestream.IsBusy");

    private readonly ICallGateSubscriber<uint> getActiveCustomAetheryte = plugin.GetIpcSubscriber<uint>("Lifestream.GetActiveCustomAetheryte");

    private readonly ICallGateSubscriber<uint, bool> aethernetTeleportByPlaceNameId = plugin.GetIpcSubscriber<uint, bool>("Lifestream.AethernetTeleportByPlaceNameId");

    private readonly ICallGateSubscriber<object> abort = plugin.GetIpcSubscriber<object>("Lifestream.Abort");

    public bool IsAvailable
    {
        get
        {
            try
            {
                return aethernetTeleportByPlaceNameId.HasFunction && isBusy.HasFunction;
            }
            catch
            {
                return false;
            }
        }
    }

    public bool IsBusy()
    {
        try
        {
            return isBusy.InvokeFunc();
        }
        catch
        {
            // Missing/broken IPC must not look "busy" or pathing waits forever.
            return false;
        }
    }

    public uint GetActiveCustomAetheryte()
    {
        try
        {
            return getActiveCustomAetheryte.InvokeFunc();
        }
        catch
        {
            return 0;
        }
    }

    public bool AethernetTeleportByPlaceNameId(uint placeNameRowId)
    {
        try
        {
            return aethernetTeleportByPlaceNameId.InvokeFunc(placeNameRowId);
        }
        catch
        {
            return false;
        }
    }

    public void Abort()
    {
        try
        {
            abort.InvokeAction();
        }
        catch
        {
        }
    }
}
