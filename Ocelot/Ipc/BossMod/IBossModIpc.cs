namespace Ocelot.Ipc.BossMod;

public interface IBossModIpc
{
    bool IsAvailable { get; }

    bool Create(string presetSerialized, bool overwrite = false);

    string? Get(string name);

    bool Delete(string name);

    bool SetActive(string name);

    bool ClearActive();

    string? GetActive();

    bool Activate(string name);

    bool Deactivate(string name);

    bool AddTransientStrategy(string presetName, string moduleTypeName, string trackName, string value);

    bool ClearTransientPresetStrategies(string presetName);
}
