using System.Reflection;
using Dalamud.Bindings.ImGui;
using Lumina.Excel;
using Ocelot.Config.Fields;
using Ocelot.Config.Renderers.Excel;
using Ocelot.Extensions;
using Ocelot.Services.Data;
using Ocelot.Services.Translation;
using Ocelot.UI;

namespace Ocelot.Config.Renderers;

public class ExcelOrderedMultiSelectRenderer<TRow, TDisplay, TFilter>(IDataRepository<TRow> data, TDisplay display, TFilter filter)
    : IFieldRenderer<ExcelOrderedMultiSelectAttribute<TRow, TDisplay, TFilter>>
    where TRow : struct, IExcelRow<TRow>
    where TDisplay : IExcelDisplay<TRow>
    where TFilter : IExcelFilter<TRow>
{
    private sealed record CachedList(string[] Labels, uint[] Keys);

    private CachedList? cache = null;

    public bool Render(object target, PropertyInfo prop, ExcelOrderedMultiSelectAttribute<TRow, TDisplay, TFilter> attr, Type owner, ITranslator translator)
    {
        if (cache == null)
        {
            var labels = new List<string>();
            var values = new List<uint>();

            foreach (var datum in data.GetAll())
            {
                if (!filter.Filter(datum))
                {
                    continue;
                }

                labels.Add(display.Display(datum));
                values.Add(datum.RowId);
            }

            cache = new CachedList(labels.ToArray(), values.ToArray());
        }

        var propType = prop.PropertyType;
        if (propType != typeof(List<uint>) && propType != typeof(HashSet<uint>) && propType != typeof(uint[]))
        {
            throw new InvalidOperationException(
                $"[ExcelOrderedMultiSelectRenderer] must be used on List<uint>, HashSet<uint>, or uint[] properties. " +
                $"{prop.DeclaringType?.Name}.{prop.Name} is {propType.Name}.");
        }

        List<uint> workingList;
        var created = false;

        if (propType == typeof(List<uint>))
        {
            workingList = (List<uint>?)prop.GetValue(target) ?? [];
            if (prop.GetValue(target) == null)
            {
                prop.SetValue(target, workingList);
                created = true;
            }
        }
        else if (propType == typeof(HashSet<uint>))
        {
            var set = (HashSet<uint>?)prop.GetValue(target) ?? [];
            workingList = set.ToList();
            if (prop.GetValue(target) == null)
            {
                prop.SetValue(target, workingList);
                created = true;
            }
        }
        else // uint[]
        {
            var arr = (uint[]?)prop.GetValue(target) ?? [];
            workingList = arr.ToList();
            if (prop.GetValue(target) == null)
            {
                prop.SetValue(target, workingList);
                created = true;
            }
        }

        var changed = created;

        if (workingList.Count != workingList.Distinct().Count())
        {
            workingList = workingList.Distinct().ToList();
            prop.SetValue(target, workingList);
            return true;
        }

        var label = prop.Label(owner, translator);

        string PreviewText()
        {
            if (workingList.Count == 0)
            {
                return translator.T("None");
            }

            var selectedLabels = new List<string>();
            for (var i = 0; i < cache!.Keys.Length && selectedLabels.Count < 3; i++)
            {
                if (workingList.Contains(cache.Keys[i]))
                {
                    selectedLabels.Add(cache.Labels[i]);
                }
            }

            if (workingList.Count <= 3)
            {
                return string.Join(", ", selectedLabels);
            }

            return $"{string.Join(", ", selectedLabels)}, +{workingList.Count - 3}";
        }

        OcelotUi.PushFieldStyle();
        try
        {
            if (ImGui.BeginCombo(label, PreviewText()))
            {
                foreach (var key in workingList.ToList())
                {
                    var first = key == workingList.First();
                    var last = key == workingList.Last();
                    var index = workingList.IndexOf(key);

                    ImGui.PushID($"###{prop.Label(owner, translator)}_{key}");
                    if (first)
                    {
                        ImGui.BeginDisabled();
                        ImGui.ArrowButton("Up", ImGuiDir.Up);
                        ImGui.SameLine();
                        ImGui.EndDisabled();
                    }
                    else
                    {
                        if (ImGui.ArrowButton("Up", ImGuiDir.Up))
                        {
                            (workingList[index - 1], workingList[index]) = (workingList[index], workingList[index - 1]);
                            changed = true;
                        }

                        ImGui.SameLine();
                    }

                    if (last)
                    {
                        ImGui.BeginDisabled();
                        ImGui.ArrowButton("Down", ImGuiDir.Down);
                        ImGui.SameLine();
                        ImGui.EndDisabled();
                    }
                    else
                    {
                        if (ImGui.ArrowButton("Down", ImGuiDir.Down))
                        {
                            (workingList[index + 1], workingList[index]) = (workingList[index], workingList[index + 1]);
                            changed = true;
                        }

                        ImGui.SameLine();
                    }

                    var l = display.Display(data.Get(key));
                    if (ImGui.Selectable(l, true, ImGuiSelectableFlags.DontClosePopups))
                    {
                        workingList.Remove(key);
                        changed = true;
                    }

                    ImGui.PopID();
                }

                for (var i = 0; i < cache!.Labels.Length; i++)
                {
                    var key = cache.Keys[i];
                    var selected = workingList.Contains(key);

                    if (!selected)
                    {
                        if (ImGui.Selectable(cache.Labels[i], selected, ImGuiSelectableFlags.DontClosePopups))
                        {
                            workingList.Add(key);
                            changed = true;
                        }
                    }
                }

                ImGui.EndCombo();
            }

            prop.Tooltip(owner, translator);

            if (changed)
            {
                if (propType == typeof(List<uint>))
                {
                    prop.SetValue(target, workingList);
                }
                else if (propType == typeof(HashSet<uint>))
                {
                    prop.SetValue(target, workingList.ToHashSet());
                }
                else // uint[]
                {
                    prop.SetValue(target, workingList.ToArray());
                }
            }

            return changed;
        }
        finally
        {
            OcelotUi.PopFieldStyle();
        }
    }
}
