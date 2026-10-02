using System.Reflection;
using Dalamud.Bindings.ImGui;
using Ocelot.Services.Translation;

namespace Ocelot.Extensions;

public static class PropertyInfoExtensions
{
    private static string GetFieldKeyBase(this PropertyInfo prop, Type owner)
    {
        var config = owner.Name.Replace("Config", "").ToSnakeCase();
        var field = prop.Name.ToSnakeCase();

        return $"config.{config}.fields.{field}";
    }

    public static string GetFieldLabelKey(this PropertyInfo prop, Type owner)
    {
        return $"{prop.GetFieldKeyBase(owner)}.label";
    }

    public static string GetFieldTooltipKey(this PropertyInfo prop, Type owner)
    {
        return $"{prop.GetFieldKeyBase(owner)}.tooltip";
    }

    public static string GetFieldTooltipDisabledKey(this PropertyInfo prop, Type owner)
    {
        return $"{prop.GetFieldKeyBase(owner)}.tooltip_disabled";
    }

    public static string Label(this PropertyInfo prop, Type owner, ITranslator translator)
    {
        var key = prop.GetFieldLabelKey(owner);
        return translator.T(key);
    }

    public static void Tooltip(this PropertyInfo prop, Type owner, ITranslator translator)
    {
        // Disabled fields (Requires / DisabledWhen) need AllowWhenDisabled or hover never fires.
        if (!ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            return;
        }

        bool disabledHover = !ImGui.IsItemHovered();
        string disabledKey = prop.GetFieldTooltipDisabledKey(owner);
        string tooltipKey = prop.GetFieldTooltipKey(owner);

        if (disabledHover && translator.Has(disabledKey))
        {
            DrawWrappedTooltip(translator.T(disabledKey));
            return;
        }

        if (!translator.Has(tooltipKey))
        {
            return;
        }

        DrawWrappedTooltip(translator.T(tooltipKey));
    }

    public static void DrawWrappedTooltip(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
}
