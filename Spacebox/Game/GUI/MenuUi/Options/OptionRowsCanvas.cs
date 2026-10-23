using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class OptionRowsCanvas : IOptionRowVisitor
{
    private const float ValueWidth = 230f;
    private const float RowHeight = 28f;

    private readonly IOptionsNavigator _options;
    private readonly List<Action> _refreshers = new List<Action>();
    private VBoxContainer _target;

    public OptionRowsCanvas(IOptionsNavigator options)
    {
        _options = options;
    }

    public void Build(VBoxContainer target, IReadOnlyList<OptionRow> rows)
    {
        _target = target;
        _refreshers.Clear();
        _disablers.Clear();
        for (int i = 0; i < rows.Count; i++) rows[i].Accept(this);
        _target = null;
        Refresh();
    }

    public void Refresh()
    {
        for (int i = 0; i < _refreshers.Count; i++) _refreshers[i]();
    }

    public void Visit(SectionRow row)
    {
        var label = new Label { Text = row.Label, ThemeTypeVariation = "SectionLabel", CustomMinimumSize = new Vector2(0f, RowHeight) };
        label.VerticalAlignment = VerticalAlignment.Bottom;
        _target.AddChild(label);
        Track(row, label, null, null);
    }

    public void Visit(SliderRow row)
    {
        var box = new HBoxContainer { CustomMinimumSize = new Vector2(ValueWidth, 0f) };
        box.AddThemeConstantOverride("separation", 8f);
        var slider = new HSlider { MinValue = row.Min, MaxValue = row.Max, Step = 1, SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand, SizeFlagsVertical = SizeFlags.ShrinkCenter, TooltipText = row.Tooltip };
        var value = new Label { CustomMinimumSize = new Vector2(78f, 0f), HorizontalAlignment = HorizontalAlignment.Right, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        box.AddChild(slider);
        box.AddChild(value);
        bool updating = false;
        int shown = int.MinValue;
        slider.ValueChanged += v =>
        {
            if (updating) return;
            int next = (int)Math.Round(v);
            value.Text = row.Format(next);
            if (!row.ApplyOnRelease) row.Set(next);
        };
        if (row.ApplyOnRelease) slider.DragEnded += () => row.Set((int)Math.Round(slider.Value));
        var line = AddLine(row, box);
        Track(row, line, () =>
        {
            if (slider.IsMouseDown) return;
            int current = row.Get();
            if (current == shown) return;
            shown = current;
            updating = true;
            slider.Value = current;
            updating = false;
            value.Text = row.Format(current);
        }, slider);
    }

    public void Visit(CheckRow row)
    {
        var check = new CheckBox("", row.Get()) { TooltipText = row.Tooltip, SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        var holder = new HBoxContainer { CustomMinimumSize = new Vector2(ValueWidth, 0f) };
        holder.AddChild(check);
        bool updating = false;
        check.Toggled += (_, value) =>
        {
            if (updating) return;
            _options.PlayClick();
            row.Set(value);
        };
        var line = AddLine(row, holder);
        Track(row, line, () =>
        {
            bool current = row.Get();
            if (check.ButtonPressed == current) return;
            updating = true;
            check.ButtonPressed = current;
            updating = false;
        }, check);
    }

    public void Visit(ChoiceRow row)
    {
        var choice = new ChoiceBox { CustomMinimumSize = new Vector2(ValueWidth, 0f), TooltipText = row.Tooltip };
        choice.SetChoices(row.Items);
        bool updating = false;
        choice.IndexChanged += index =>
        {
            if (updating) return;
            _options.PlayClick();
            row.Set(index);
        };
        var line = AddLine(row, choice);
        Track(row, line, () =>
        {
            int current = row.Get();
            if (choice.SelectedIndex == current) return;
            updating = true;
            choice.SelectedIndex = current;
            updating = false;
        }, null);
        _disablers[line] = disabled => choice.Disabled = disabled;
    }

    public void Visit(ButtonRow row)
    {
        var button = new Button { Text = row.Text, CustomMinimumSize = new Vector2(ValueWidth, 0f), TooltipText = row.Tooltip };
        button.Pressed += _ =>
        {
            _options.PlayClick();
            row.Click();
        };
        var line = AddLine(row, button);
        Track(row, line, null, button);
    }

    public void Visit(TextRow row)
    {
        var edit = new LineEdit { MaxLength = row.MaxLength, CustomMinimumSize = new Vector2(ValueWidth, 0f), TooltipText = row.Tooltip, Text = row.Get() ?? "" };
        edit.TextChanged += text => row.Set(text);
        var line = AddLine(row, edit);
        Track(row, line, null, null);
    }

    private readonly Dictionary<Control, Action<bool>> _disablers = new Dictionary<Control, Action<bool>>();

    private HBoxContainer AddLine(OptionRow row, Control value)
    {
        var line = new HBoxContainer { CustomMinimumSize = new Vector2(0f, RowHeight) };
        line.AddThemeConstantOverride("separation", 10f);
        var label = new Label { Text = row.Label, TooltipText = row.Tooltip, SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand, SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilter.Stop };
        value.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        line.AddChild(label);
        line.AddChild(value);
        _target.AddChild(line);
        return line;
    }

    private void Track(OptionRow row, Control line, Action sync, Control disableable)
    {
        _refreshers.Add(() =>
        {
            bool visible = row.IsVisible;
            if (line.Visible != visible) line.Visible = visible;
            if (!visible) return;
            bool disabled = !row.IsEnabled;
            if (disableable is BaseButton button) button.Disabled = disabled;
            else if (disableable != null) disableable.MouseFilter = disabled ? MouseFilter.Ignore : MouseFilter.Stop;
            if (_disablers.TryGetValue(line, out var setDisabled)) setDisabled(disabled);
            line.Modulate = disabled ? new Color4(1f, 1f, 1f, 0.5f) : Color4.White;
            sync?.Invoke();
        });
    }
}
