using Ocelot.Services;

namespace Ocelot.Lifecycle;

public interface IOrderedHook
{
    int Order
    {
        get => 0;
    }
}

[OcelotAutoWire]
public interface IOnLoad : IOrderedHook
{
    void OnLoad();
}

[OcelotAutoWire]
public interface IOnStart : IOrderedHook
{
    void OnStart();
}

[OcelotAutoWire]
public interface IOnStop : IOrderedHook
{
    void OnStop();
}

[OcelotAutoWire]
public interface IOnPreUpdate : IOrderedHook
{
    UpdateLimit UpdateLimit
    {
        get => UpdateLimit.None;
    }

    void PreUpdate();
}

[OcelotAutoWire]
public interface IOnUpdate : IOrderedHook
{
    UpdateLimit UpdateLimit
    {
        get => UpdateLimit.None;
    }

    void Update();
}

[OcelotAutoWire]
public interface IOnPostUpdate : IOrderedHook
{
    UpdateLimit UpdateLimit
    {
        get => UpdateLimit.None;
    }

    void PostUpdate();
}

[OcelotAutoWire]
public interface IOnPreRender : IOrderedHook
{
    void PreRender();
}

[OcelotAutoWire]
public interface IOnRender : IOrderedHook
{
    void Render();
}

[OcelotAutoWire]
public interface IOnPostRender : IOrderedHook
{
    void PostRender();
}

[OcelotAutoWire]
public interface IOnTerritoryChanged : IOrderedHook
{
    void OnTerritoryChanged(uint territory);
}
