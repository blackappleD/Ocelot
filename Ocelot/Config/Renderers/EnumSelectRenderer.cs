using System.Reflection;
using Dalamud.Bindings.ImGui;
using Ocelot.Config.Fields;
using Ocelot.Config.Renderers.Enum;
using Ocelot.Extensions;
using Ocelot.Services.Translation;
using Ocelot.UI;

namespace Ocelot.Config.Renderers;

public class EnumSelectRenderer<TEnum, TDisplay, TFilter>(TDisplay display, TFilter filter)
    : IFieldRenderer<EnumSelectAttribute<TEnum, TDisplay, TFilter>>
    where TEnum : struct, System.Enum
    where TDisplay : IEnumDisplay<TEnum>
    where TFilter : IEnumFilter<TEnum>
{
    private sealed record CachedList(string[] Labels, TEnum[] Keys);

    private CachedList? cache = null;

    public bool Render(object target, PropertyInfo prop, EnumSelectAttribute<TEnum, TDisplay, TFilter> attr, Type owner, ITranslator translator)
    {
        var labels = new List<string>();
        var values = new List<TEnum>();

        foreach (var datum in System.Enum.GetValues<TEnum>())
        {
            if (!filter.Filter(datum))
            {
                continue;
            }

            labels.Add(display.Display(datum));
            values.Add(datum);
        }

        if (cache == null
            || cache.Keys.Length != values.Count
            || !cache.Keys.SequenceEqual(values))
        {
            cache = new CachedList(labels.ToArray(), values.ToArray());
        }

        if (prop.PropertyType != typeof(TEnum))
        {
            throw new InvalidOperationException(
                $"[EnumSelectRenderer] can only be used on {nameof(TEnum)} properties. {prop.DeclaringType?.Name}.{prop.Name} is {prop.PropertyType.Name}.");
        }

        var value = (TEnum)(prop.GetValue(target) ?? 0);

        var index = Array.IndexOf(cache.Keys, value);
        if (index < 0)
        {
            index = 0;
        }

        var label = prop.Label(owner, translator);

        OcelotUi.PushFieldStyle();
        try
        {
            var changed = ImGui.Combo(label, ref index, cache.Labels, cache.Labels.Length);

            prop.Tooltip(owner, translator);

            if (changed)
            {
                var selectedKey = cache.Keys[index];
                prop.SetValue(target, selectedKey);
            }

            return changed;
        }
        finally
        {
            OcelotUi.PopFieldStyle();
        }
    }
}
