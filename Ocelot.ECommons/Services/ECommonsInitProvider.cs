using ECommons;

namespace Ocelot.ECommons.Services;

public interface IECommonsInitProvider
{
    Module[] GetModules();
}

public class ECommonsInitProvider : IECommonsInitProvider
{
    public Module[] GetModules()
    {
        // Avoid Module.All — VfxTracking/ObjectLife/SplatoonAPI install Reloaded hooks we don't use
        // and exhaust trampoline space after repeated AutomaticReloading (MemoryBuffer errors).
        return [Module.ObjectFunctions];
    }
}
