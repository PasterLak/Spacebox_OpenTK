using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Ships;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class ShipConsoleWindow : GameWindow
{
    private const float Width = 380f;
    private const float SwatchSize = 16f;

    private readonly Label _parts = new Label();
    private readonly Label _speed = new Label();
    private readonly Label _flightHint = new Label { ThemeTypeVariation = "MutedLabel", Autowrap = true, CustomMinimumSize = new Vector2(Width, 0f), Text = "Movement keys fly, roll keys roll, mouse steers (hold Alt to look around), sprint brakes" };
    private readonly VBoxContainer _groups = new VBoxContainer();
    private readonly List<(Label Count, Button Key)> _groupRows = new List<(Label, Button)>();
    private readonly Label _camera = StatusText.Create("", StatusKind.Muted);
    private int _builtGroups = -1;

    public ShipConsoleWindow() : base("Ship Console")
    {
        Name = "ShipConsole";
        Body.AddChild(_parts);
        Body.AddChild(_speed);
        Body.AddChild(_flightHint);
        Body.AddChild(new HSeparator());
        Body.AddChild(_groups);
        Body.AddChild(_camera);
        Body.AddChild(new HSeparator());

        var actions = Body.AddChild(new HBoxContainer());
        var bind = actions.AddChild(new Button { Text = "Bind blocks", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        bind.Pressed += _ => ShipConsoleUI.BindBlocks();
        var leave = actions.AddChild(new Button { Text = "Leave seat", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        leave.Pressed += _ => ShipConsoleUI.LeaveSeat();
        Body.AddChild(new Label { Text = "Binding: LMB or mouse wheel changes group (camera: toggle), RMB exits", ThemeTypeVariation = "MutedLabel", Autowrap = true, CustomMinimumSize = new Vector2(Width, 0f) });
    }

    public override bool IsOpen => ShipConsoleUI.IsOpen && ShipConsoleUI.Bindings != null;

    protected override void Refresh()
    {
        var console = ShipConsoleUI.Console;
        var bindings = ShipConsoleUI.Bindings;
        string title = GameAssets.GetBlockDataById(console.Id)?.Name ?? "Ship Console";
        if (TitleLabel.Text != title) TitleLabel.Text = title;

        var control = console.Entity.ShipControl;
        float speed = console.Entity.Ship?.Motion.Velocity.Length ?? 0f;
        SetText(_parts, $"Thrusters: {control.Thrusters.Count}   Cannons: {control.Cannons.Count}   Mass: {console.Entity.Mass:0}");
        SetText(_speed, $"Speed: {speed:0.0} m/s");
        _flightHint.Visible = console.Data.Flight;

        if (bindings.GroupCount != _builtGroups) BuildGroups(bindings.GroupCount);
        var counts = ShipConsoleUI.CountGroups(bindings);
        for (int group = 0; group < _groupRows.Count; group++)
        {
            var (count, key) = _groupRows[group];
            SetText(count, $"Group {group + 1}: {counts[group]} blocks");
            bool waiting = ShipConsoleUI.WaitingGroup == group;
            string label = waiting ? "Press a key..." : ShipKeys.GetName(bindings.GetKey(group));
            if (key.Text != label) key.Text = label;
        }
        StatusText.Set(_camera, bindings.Camera.HasValue ? "Camera: bound" : "Camera: not bound", StatusKind.Muted);
    }

    private void BuildGroups(int count)
    {
        _builtGroups = count;
        _groups.ClearChildren();
        _groupRows.Clear();
        for (int group = 0; group < count; group++)
        {
            int index = group;
            var row = _groups.AddChild(new HBoxContainer());
            var color = ConsoleBindings.GroupColors[group % ConsoleBindings.GroupColors.Length];
            row.AddChild(new ColorRect { Color = new Color4(color.R, color.G, color.B, 1f), CustomMinimumSize = new Vector2(SwatchSize, SwatchSize), SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var label = row.AddChild(new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center });
            var key = row.AddChild(new Button { ThemeTypeVariation = "SmallButton", CustomMinimumSize = new Vector2(140f, 0f), TooltipText = "Click, then press the key for this group" });
            key.Pressed += _ => ShipConsoleUI.ToggleWaiting(index);
            _groupRows.Add((label, key));
        }
    }

    private static void SetText(Label label, string text)
    {
        if (label.Text != text) label.Text = text;
    }
}
