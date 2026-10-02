using System.Numerics;
using Dalamud.Bindings.ImGui;
using Ocelot.Graphics;

namespace Ocelot.Services.UI;

public interface IBrandingService
{
    Vector2 WindowPadding { get; }

    Vector2 FramePadding { get; }

    Vector2 CellPadding { get; }

    Vector2 ItemSpacing { get; }

    Vector2 ItemInnerSpacing { get; }

    float IndentSpacing { get; }

    float ScrollbarSize { get; }

    float GrabMinSize { get; }

    float WindowBorderSize { get; }

    float ChildBorderSize { get; }

    float PopupBorderSize { get; }

    float FrameBorderSize { get; }

    float TabBorderSize { get; }

    float WindowRounding { get; }

    float ChildRounding { get; }

    float FrameRounding { get; }

    float PopupRounding { get; }

    float ScrollbarRounding { get; }

    float GrabRounding { get; }

    float TabRounding { get; }

    Vector2 WindowTitleAlign { get; }

    Vector2 ButtonTextAlign { get; }

    Vector2 SelectableTextAlign { get; }

    Vector2 DisplaySafeAreaPadding { get; }

    Color this[ImGuiCol col] { get; }

    Color Text { get; }

    Color TextDisabled { get; }

    Color WindowBg { get; }

    Color ChildBg { get; }

    Color PopupBg { get; }

    Color Border { get; }

    Color FrameBg { get; }

    Color FrameBgHovered { get; }

    Color FrameBgActive { get; }

    Color TitleBg { get; }

    Color TitleBgActive { get; }

    Color TitleBgCollapsed { get; }

    Color MenuBarBg { get; }

    Color ScrollbarBg { get; }

    Color ScrollbarGrab { get; }

    Color ScrollbarGrabHovered { get; }

    Color ScrollbarGrabActive { get; }

    Color CheckMark { get; }

    Color SliderGrab { get; }

    Color SliderGrabActive { get; }

    Color Button { get; }

    Color ButtonHovered { get; }

    Color ButtonActive { get; }

    Color Header { get; }

    Color HeaderHovered { get; }

    Color HeaderActive { get; }

    Color Separator { get; }

    Color SeparatorHovered { get; }

    Color SeparatorActive { get; }

    Color ResizeGrip { get; }

    Color ResizeGripHovered { get; }

    Color ResizeGripActive { get; }

    Color Tab { get; }

    Color TabHovered { get; }

    Color TabActive { get; }

    Color TabUnfocused { get; }

    Color TabUnfocusedActive { get; }

    Color DockingPreview { get; }

    Color DockingEmptyBg { get; }

    Color TableHeaderBg { get; }

    Color TableBorderStrong { get; }

    Color TableBorderLight { get; }

    Color TableRowBg { get; }

    Color TableRowBgAlt { get; }

    Color TextSelectedBg { get; }

    Color DragDropTarget { get; }

    Color NavHighlight { get; }

    Color NavWindowingHighlight { get; }

    Color NavWindowingDimBg { get; }

    Color ModalWindowDimBg { get; }

    Color DalamudRed { get; }

    Color DalamudGrey { get; }

    Color DalamudGrey2 { get; }

    Color DalamudGrey3 { get; }

    Color DalamudWhite { get; }

    Color DalamudWhite2 { get; }

    Color DalamudOrange { get; }

    Color TankBlue { get; }

    Color HealerGreen { get; }

    Color DPSRed { get; }

    Color DalamudYellow { get; }

    Color DalamudViolet { get; }

    Color ParsedGrey { get; }

    Color ParsedGreen { get; }

    Color ParsedBlue { get; }

    Color ParsedPurple { get; }

    Color ParsedOrange { get; }

    Color ParsedPink { get; }
}
