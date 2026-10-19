using System.Numerics;
using ImGuiNET;

namespace Spacebox.Game.GUI.Menu;

public sealed class OptionsRows
{
    private readonly float _valueWidth;
    private readonly Vector2 _checkboxOffset;

    private OptionsRows(float totalWidth)
    {
        _valueWidth = totalWidth * 0.38f;
        _checkboxOffset = new Vector2(_valueWidth * 0.5f - ImGui.GetFrameHeight() * 0.5f, 0f);
    }

    public static OptionsRows Begin(string id, float totalWidth)
    {
        ImGui.BeginTable(id, 2, ImGuiTableFlags.NoBordersInBody);
        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, totalWidth * 0.6f);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, totalWidth * 0.4f);
        return new OptionsRows(totalWidth);
    }

    public void End()
    {
        ImGui.EndTable();
    }

    public void Section(string title)
    {
        ImGui.TableNextRow(ImGuiTableRowFlags.None, ImGui.GetFrameHeight() * 1.6f);
        ImGui.TableNextColumn();
        ImGui.Dummy(new Vector2(0f, ImGui.GetTextLineHeight() * 0.4f));
        ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), title);
        ImGui.TableNextColumn();
    }

    public bool Slider(string label, string tooltip, ref int value, int min, int max, string format = "%d")
    {
        Label(label, tooltip);
        ImGui.SetNextItemWidth(_valueWidth);
        bool changed = ImGui.SliderInt("##" + label, ref value, min, max, format, ImGuiSliderFlags.AlwaysClamp);
        UIHelper.ShowTooltip(tooltip);
        return changed;
    }

    public bool Checkbox(string label, string tooltip, ref bool value)
    {
        Label(label, tooltip);
        ImGui.Dummy(_checkboxOffset);
        ImGui.SameLine();
        bool changed = ImGui.Checkbox("##" + label, ref value);
        UIHelper.ShowTooltip(tooltip);
        return changed;
    }

    public bool Combo(string label, string tooltip, ref int index, string[] items)
    {
        Label(label, tooltip);
        ImGui.SetNextItemWidth(_valueWidth);
        bool changed = ImGui.Combo("##" + label, ref index, items, items.Length);
        UIHelper.ShowTooltip(tooltip);
        return changed;
    }

    public static bool LastItemReleased => ImGui.IsItemDeactivatedAfterEdit();

    private static void Label(string label, string tooltip)
    {
        ImGui.TableNextRow(ImGuiTableRowFlags.None, ImGui.GetFrameHeight() * 1.45f);
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.Text(label);
        UIHelper.ShowTooltip(tooltip);
        ImGui.TableNextColumn();
    }
}
