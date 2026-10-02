namespace Ocelot.Ipc.PandorasBox;

public interface IPandorasBoxIpc
{
    bool IsAvailable { get; }

    bool? GetFeatureEnabledInternal(string internalName);

    void SetFeatureEnabledInternal(string internalName, bool enabled);
}
