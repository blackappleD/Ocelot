using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace Ocelot.Ipc.RotationSolverReborn;

public class RotationSolverRebornIpc(IDalamudPluginInterface plugin) : IRotationSolverRebornIpc
{
    // ChangeOperatingMode is an Action (void). HasFunction stays false — 4.1.0.4 never called Henched.
    private readonly ICallGateSubscriber<RSRStateCommandType, object> changeOperatingMode = plugin
        .GetIpcSubscriber<RSRStateCommandType, object>("RotationSolverReborn.ChangeOperatingMode");

    private readonly ICallGateSubscriber<byte, object> changeOperatingModeByte = plugin
        .GetIpcSubscriber<byte, object>("RotationSolverReborn.ChangeOperatingMode");

    public bool IsAvailable
    {
        get
        {
            try
            {
                return changeOperatingMode.HasAction || changeOperatingModeByte.HasAction;
            }
            catch
            {
                return false;
            }
        }
    }

    public bool ChangeOperatingMode(RSRStateCommandType command)
    {
        try
        {
            if (changeOperatingMode.HasAction)
            {
                try
                {
                    changeOperatingMode.InvokeAction(command);
                    return true;
                }
                catch
                {
                    return command == RSRStateCommandType.Off;
                }
            }

            if (changeOperatingModeByte.HasAction)
            {
                try
                {
                    changeOperatingModeByte.InvokeAction((byte)command);
                    return true;
                }
                catch
                {
                    return command == RSRStateCommandType.Off;
                }
            }
        }
        catch
        {
        }

        return false;
    }
}
