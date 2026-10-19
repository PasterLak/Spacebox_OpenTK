using System.Numerics;
using Engine.Graphics.Atlas;
using ImGuiNET;

namespace Spacebox.Editor.UI;

public static class EditorWidgets
{
    private static readonly Dictionary<string, string> Filters = new Dictionary<string, string>();

    public static float Scale => Math.Clamp(ImGui.GetIO().DisplaySize.Y / 900f, 1f, 2.5f);
    public static float IconSize => MathF.Round(20f * Scale);
    public static float RowHeight => ImGui.GetFrameHeight();

    public static void Icon(AtlasSprite sprite, float size)
    {
        if (sprite.IsValid) sprite.Image(new Vector2(size, size));
        else ImGui.Dummy(new Vector2(size, size));
    }

    public static bool SearchCombo(string id, ref string value, IReadOnlyList<string> options, Func<string, AtlasSprite> icon = null, string emptyLabel = "(none)")
    {
        bool changed = false;
        float iconSize = ImGui.GetTextLineHeight();

        if (icon != null)
        {
            Icon(icon(value), iconSize);
            ImGui.SameLine();
        }

        ImGui.SetNextItemWidth(-1f);
        string preview = string.IsNullOrEmpty(value) ? emptyLabel : value;
        if (!ImGui.BeginCombo(id, preview, ImGuiComboFlags.HeightLarge)) return false;

        if (!Filters.TryGetValue(id, out var filter)) filter = "";
        if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere();
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputTextWithHint("##filter", "Search or type a value", ref filter, 128)) Filters[id] = filter;

        if (!string.IsNullOrWhiteSpace(filter) && ImGui.Selectable($"Use \"{filter}\""))
        {
            value = filter;
            changed = true;
        }

        if (ImGui.Selectable(emptyLabel, string.IsNullOrEmpty(value)))
        {
            value = "";
            changed = true;
        }

        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            if (!string.IsNullOrEmpty(filter) && option.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

            ImGui.PushID(i);
            if (icon != null)
            {
                Icon(icon(option), iconSize);
                ImGui.SameLine();
            }

            if (ImGui.Selectable(option, option == value))
            {
                value = option;
                changed = true;
            }
            ImGui.PopID();
        }

        ImGui.EndCombo();
        if (changed) Filters[id] = "";
        return changed;
    }

    public static bool ToolbarButton(string label, bool enabled = true)
    {
        if (!enabled) ImGui.BeginDisabled();
        bool pressed = ImGui.Button(label);
        if (!enabled) ImGui.EndDisabled();
        return pressed && enabled;
    }

    public static void Tooltip(string text)
    {
        if (!string.IsNullOrEmpty(text) && ImGui.IsItemHovered(ImGuiHoveredFlags.DelayShort)) ImGui.SetTooltip(text);
    }

    public static void Header(string text)
    {
        ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), text);
        ImGui.Separator();
    }
}
