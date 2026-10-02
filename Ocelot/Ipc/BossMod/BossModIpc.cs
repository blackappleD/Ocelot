using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace Ocelot.Ipc.BossMod;

public class BossModIpc(IDalamudPluginInterface plugin) : IBossModIpc
{
    private readonly ICallGateSubscriber<string, bool, bool> create =
        plugin.GetIpcSubscriber<string, bool, bool>("BossMod.Presets.Create");

    private readonly ICallGateSubscriber<string, string?> get =
        plugin.GetIpcSubscriber<string, string?>("BossMod.Presets.Get");

    private readonly ICallGateSubscriber<string, bool> delete =
        plugin.GetIpcSubscriber<string, bool>("BossMod.Presets.Delete");

    private readonly ICallGateSubscriber<string, bool> setActive =
        plugin.GetIpcSubscriber<string, bool>("BossMod.Presets.SetActive");

    private readonly ICallGateSubscriber<bool> clearActive =
        plugin.GetIpcSubscriber<bool>("BossMod.Presets.ClearActive");

    private readonly ICallGateSubscriber<string?> getActive =
        plugin.GetIpcSubscriber<string?>("BossMod.Presets.GetActive");

    private readonly ICallGateSubscriber<string, bool> activate =
        plugin.GetIpcSubscriber<string, bool>("BossMod.Presets.Activate");

    private readonly ICallGateSubscriber<string, bool> deactivate =
        plugin.GetIpcSubscriber<string, bool>("BossMod.Presets.Deactivate");

    private readonly ICallGateSubscriber<string, string, string, string, bool> addTransient =
        plugin.GetIpcSubscriber<string, string, string, string, bool>("BossMod.Presets.AddTransientStrategy");

    private readonly ICallGateSubscriber<string, bool> clearTransient =
        plugin.GetIpcSubscriber<string, bool>("BossMod.Presets.ClearTransientPresetStrategies");

    public bool IsAvailable
    {
        get
        {
            try
            {
                return create.HasFunction && setActive.HasFunction && clearActive.HasFunction;
            }
            catch
            {
                return false;
            }
        }
    }

    public bool Create(string presetSerialized, bool overwrite = false)
    {
        try
        {
            return create.HasFunction && create.InvokeFunc(presetSerialized, overwrite);
        }
        catch
        {
            return false;
        }
    }

    public string? Get(string name)
    {
        try
        {
            return get.HasFunction ? get.InvokeFunc(name) : null;
        }
        catch
        {
            return null;
        }
    }

    public bool Delete(string name)
    {
        try
        {
            return delete.HasFunction && delete.InvokeFunc(name);
        }
        catch
        {
            return false;
        }
    }

    public bool SetActive(string name)
    {
        try
        {
            return setActive.HasFunction && setActive.InvokeFunc(name);
        }
        catch
        {
            return false;
        }
    }

    public bool ClearActive()
    {
        try
        {
            return clearActive.HasFunction && clearActive.InvokeFunc();
        }
        catch
        {
            return false;
        }
    }

    public string? GetActive()
    {
        try
        {
            return getActive.HasFunction ? getActive.InvokeFunc() : null;
        }
        catch
        {
            return null;
        }
    }

    public bool Activate(string name)
    {
        // Prefer Activate; some BossMod builds return false without enabling AI (#182).
        try
        {
            if (activate.HasFunction && activate.InvokeFunc(name))
            {
                return true;
            }
        }
        catch
        {
        }

        return SetActive(name);
    }

    public bool Deactivate(string name)
    {
        try
        {
            if (deactivate.HasFunction && deactivate.InvokeFunc(name))
            {
                return true;
            }
        }
        catch
        {
        }

        string? active = GetActive();
        if (active == null || !string.Equals(active, name, StringComparison.Ordinal))
        {
            return true;
        }

        return ClearActive();
    }

    public bool AddTransientStrategy(string presetName, string moduleTypeName, string trackName, string value)
    {
        try
        {
            return addTransient.HasFunction && addTransient.InvokeFunc(presetName, moduleTypeName, trackName, value);
        }
        catch
        {
            return false;
        }
    }

    public bool ClearTransientPresetStrategies(string presetName)
    {
        try
        {
            return clearTransient.HasFunction && clearTransient.InvokeFunc(presetName);
        }
        catch
        {
            return false;
        }
    }
}
