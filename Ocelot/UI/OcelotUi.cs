using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Ocelot.UI;

public static class OcelotUi
{
    public static readonly Vector4 Header = new(0.85f, 0.72f, 0.35f, 1f);
    public static readonly Vector4 Muted = new(0.65f, 0.65f, 0.65f, 1f);

    public const float FrameRounding = 4f;
    public static readonly Vector2 FramePadding = new(8f, 4f);
    public static readonly Vector2 ItemSpacing = new(8f, 6f);

    public static void PushFieldStyle()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, FrameRounding);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, FramePadding);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ItemSpacing);
    }

    public static void PopFieldStyle()
    {
        ImGui.PopStyleVar(3);
    }
}
