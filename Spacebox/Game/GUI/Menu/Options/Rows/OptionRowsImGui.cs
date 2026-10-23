using ImGuiNET;

namespace Spacebox.Game.GUI.Menu;

public sealed class OptionRowsImGui : IOptionRowVisitor
{
    private readonly Dictionary<SliderRow, int> _pending = new Dictionary<SliderRow, int>();
    private OptionsRows _rows;

    public void Draw(string id, IReadOnlyList<OptionRow> rows, float width)
    {
        _rows = OptionsRows.Begin(id, width);
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (!row.IsVisible) continue;
            bool disabled = !row.IsEnabled;
            if (disabled) ImGui.BeginDisabled();
            row.Accept(this);
            if (disabled) ImGui.EndDisabled();
        }
        _rows.End();
        _rows = null;
    }

    public void Visit(SectionRow row) => _rows.Section(row.Label);

    public void Visit(SliderRow row)
    {
        int value = _pending.TryGetValue(row, out var pending) ? pending : row.Get();
        bool changed = _rows.Slider(row.Label, row.Tooltip, ref value, row.Min, row.Max, row.Format(value).Replace("%", "%%"));
        if (!row.ApplyOnRelease)
        {
            if (changed) row.Set(value);
            return;
        }

        if (changed) _pending[row] = value;
        if (!OptionsRows.LastItemReleased || !_pending.Remove(row, out var released)) return;
        row.Set(released);
    }

    public void Visit(CheckRow row)
    {
        bool value = row.Get();
        if (_rows.Checkbox(row.Label, row.Tooltip, ref value)) row.Set(value);
    }

    public void Visit(ChoiceRow row)
    {
        int index = row.Get();
        if (_rows.Combo(row.Label, row.Tooltip, ref index, row.Items)) row.Set(index);
    }

    public void Visit(ButtonRow row)
    {
        if (_rows.Button(row.Label, row.Tooltip, row.Text)) row.Click();
    }

    public void Visit(TextRow row)
    {
        string value = row.Get() ?? "";
        if (_rows.TextInput(row.Label, row.Tooltip, ref value, (uint)row.MaxLength)) row.Set(value);
    }
}
