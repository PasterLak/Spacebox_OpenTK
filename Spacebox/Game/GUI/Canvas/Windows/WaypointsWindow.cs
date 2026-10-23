using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Canvas.Items;
using Spacebox.Game.GUI.MenuUi;
using ColorSwatch = Spacebox.Game.GUI.Canvas.Items.ColorSwatch;
using Spacebox.Game.Waypoints;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class WaypointsWindow : GameWindow
{
    private const float ListWidth = 240f;
    private const float EditorWidth = 240f;
    private const float ColumnGap = 8f;
    private const float ContentHeight = 250f;
    private const float SwatchSize = 18f;
    private const float MarkerSize = 10f;

    private readonly VBoxContainer _rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

    private readonly WaypointWindowUI _waypoints;
    private readonly List<(SelectableRow Row, Label Distance, Tag Tag)> _rowItems = new List<(SelectableRow, Label, Tag)>();
    private readonly Label _empty = StatusText.Create("No waypoints yet. Fly to a place you want to find again and press \"Add at my position\".", StatusKind.Muted);
    private readonly VBoxContainer _editor = new VBoxContainer { CustomMinimumSize = new Vector2(EditorWidth, 0f) };
    private readonly Label _noSelection = StatusText.Create("Select a waypoint to change its name and color.", StatusKind.Muted);
    private readonly LineEdit _name = new LineEdit { MaxLength = WaypointDefaults.MaxNameLength };
    private readonly HFlowContainer _palette = new HFlowContainer();
    private readonly List<ColorSwatch> _swatches = new List<ColorSwatch>();
    private readonly ColorPickerButton _custom = new ColorPickerButton();
    private readonly CheckBox _visible = new CheckBox("Show marker") { CustomMinimumSize = new Vector2(0f, 22f) };
    private readonly Label _distance = StatusText.Create("", StatusKind.Muted);
    private readonly Button _delete = new Button { Text = "Delete" };
    private string _structure;
    private Tag _shown;
    private bool _syncing;

    public WaypointsWindow(WaypointWindowUI waypoints) : base("Waypoints")
    {
        _waypoints = waypoints;
        Name = "Waypoints";
        Body.AddChild(StatusText.Create("Only you can see your waypoints.", StatusKind.Muted));

        var content = Body.AddChild(new HBoxContainer());
        content.AddThemeConstantOverride("separation", ColumnGap);
        var listColumn = content.AddChild(new VBoxContainer { CustomMinimumSize = new Vector2(ListWidth, ContentHeight) });
        listColumn.AddChild(MenuLayout.ScrollList(_rows, ContentHeight));
        _rows.AddThemeConstantOverride("separation", 2f);
        _empty.Autowrap = true;
        _empty.CustomMinimumSize = new Vector2(ListWidth - 30f, 0f);

        content.AddChild(_editor);
        _editor.AddThemeConstantOverride("separation", 6f);
        _noSelection.Autowrap = true;
        _noSelection.CustomMinimumSize = new Vector2(EditorWidth, 0f);
        _editor.AddChild(_noSelection);
        _editor.AddChild(MenuLayout.Field("Name", _name));
        _editor.AddChild(MenuLayout.Caption("Color"));
        _editor.AddChild(_palette);
        _palette.AddThemeConstantOverride("h_separation", 4f);
        _palette.AddThemeConstantOverride("v_separation", 4f);
        foreach (var color in WaypointDefaults.Palette)
        {
            var swatch = _palette.AddChild(new ColorSwatch(color, SwatchSize, ""));
            var picked = color;
            swatch.Clicked += () => _waypoints.SetColor(picked);
            _swatches.Add(swatch);
        }

        var customRow = _editor.AddChild(new HBoxContainer());
        customRow.AddChild(_custom);
        customRow.AddChild(StatusText.Create("Custom color", StatusKind.Muted));
        _editor.AddChild(_visible);
        _editor.AddChild(_distance);
        _editor.AddChild(_delete);

        _name.TextChanged += text =>
        {
            if (!_syncing) _waypoints.Rename(text);
        };
        _name.EditingFinished += _ => _waypoints.Save();
        _custom.ColorChanged += color =>
        {
            if (!_syncing) _waypoints.SetColor(new Color4(color.R, color.G, color.B, 1f));
        };
        _visible.Toggled += (_, pressed) =>
        {
            if (!_syncing) _waypoints.SetVisible(pressed);
        };
        _delete.Pressed += _ => _waypoints.Delete();

        var footer = Body.AddChild(new HBoxContainer());
        footer.AddThemeConstantOverride("separation", ColumnGap);
        var add = footer.AddChild(new Button { Text = "Add at my position", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        add.Pressed += _ => _waypoints.AddAtPlayer();
        var close = footer.AddChild(new Button { Text = "Close", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        close.Pressed += _ => _waypoints.Close();
    }

    public override bool IsOpen => _waypoints.IsOpen;

    protected override void Refresh()
    {
        var waypoints = _waypoints.Collect();
        string structure = Structure(waypoints);
        if (structure != _structure)
        {
            _structure = structure;
            BuildRows(waypoints);
        }

        foreach (var (row, distance, tag) in _rowItems)
        {
            row.IsSelected = ReferenceEquals(tag, _waypoints.Selected);
            SetText(distance, _waypoints.DistanceTo(tag));
        }

        RefreshEditor();
    }

    private void RefreshEditor()
    {
        var selected = _waypoints.Selected;
        bool hasSelection = selected != null;
        var children = _editor.Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is not Control control) continue;
            bool show = ReferenceEquals(control, _noSelection) ? !hasSelection : hasSelection;
            if (control.Visible != show) control.Visible = show;
        }

        if (!hasSelection)
        {
            _shown = null;
            return;
        }

        _syncing = true;
        if (!ReferenceEquals(selected, _shown))
        {
            _shown = selected;
            _name.Text = selected.Text;
        }
        else if (!_name.HasFocus && _name.Text != selected.Text) _name.Text = selected.Text;
        foreach (var swatch in _swatches) swatch.Selected = SameColor(swatch.Color, selected.Color);
        if (!SameColor(_custom.Color, selected.Color)) _custom.Color = selected.Color;
        if (_visible.ButtonPressed != selected.Visible) _visible.ButtonPressed = selected.Visible;
        _syncing = false;

        SetText(_distance, "Distance: " + _waypoints.DistanceTo(selected));
        string delete = _waypoints.ConfirmDelete ? "Click again to delete" : "Delete";
        if (_delete.Text != delete) _delete.Text = delete;
    }

    private void BuildRows(IReadOnlyList<Tag> waypoints)
    {
        _rows.ClearChildren();
        _rowItems.Clear();
        if (waypoints.Count == 0)
        {
            _rows.AddChild(_empty);
            return;
        }

        foreach (var tag in waypoints)
        {
            var row = _rows.AddChild(new SelectableRow(0f) { Metadata = tag });
            var marker = new ColorSwatch(new Color4(tag.Color.R, tag.Color.G, tag.Color.B, 1f), MarkerSize, "") { MouseFilter = MouseFilter.Ignore, SizeFlagsVertical = SizeFlags.ShrinkCenter };
            row.Row.InsertChild(0, marker);
            var name = row.AddLine(tag.Visible ? tag.Text : tag.Text + " (hidden)", tag.Visible ? "Label" : "MutedLabel");
            name.Overrun = TextOverrun.Ellipsis;
            var distance = row.Row.AddChild(StatusText.Create("", StatusKind.Muted));
            distance.VerticalAlignment = VerticalAlignment.Center;
            row.Selected += r => _waypoints.Select((Tag)r.Metadata);
            _rowItems.Add((row, distance, tag));
        }
    }

    private static string Structure(IReadOnlyList<Tag> waypoints)
    {
        var builder = new System.Text.StringBuilder();
        foreach (var tag in waypoints) builder.Append(tag.Id).Append(tag.Text).Append(tag.Visible).Append(tag.ColorUint).Append('|');
        return builder.ToString();
    }

    private static bool SameColor(Color4 a, Color4 b) => MathF.Abs(a.R - b.R) < 0.004f && MathF.Abs(a.G - b.G) < 0.004f && MathF.Abs(a.B - b.B) < 0.004f;

    private static void SetText(Label label, string text)
    {
        if (label.Text != text) label.Text = text;
    }
}
