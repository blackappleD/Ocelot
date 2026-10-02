using Dalamud.Interface.Windowing;

namespace Ocelot.Windows;

public interface IWindow
{
    string WindowName { get; }

    bool IsOpen { get; set; }

    void Toggle();
}

public interface IMainWindow : IWindow;

public interface IConfigWindow : IWindow;

public interface IMainWindowTitleBarContributor
{
    void Contribute(ICollection<TitleBarButton> buttons);
}
