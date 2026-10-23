using Engine.CanvasUI;
using Engine.Components;
using OpenTK.Mathematics;

namespace Engine.UI;

public sealed class DevNodeInspectorSection : DevPanelSection
{
    private readonly Label _name = new Label { Overrun = TextOverrun.Ellipsis };
    private readonly CheckBox _enabled = new CheckBox("Enabled");
    private readonly NumberEdit[] _position = CreateVector(0.1);
    private readonly NumberEdit[] _rotation = CreateVector(1);
    private readonly NumberEdit[] _scale = CreateVector(0.1);
    private readonly VBoxContainer _components = new VBoxContainer();
    private readonly List<Component> _shownComponents = new List<Component>();
    private Node3D _node;

    public DevNodeInspectorSection() : base("Node")
    {
        Content.AddChild(_name);
        Content.AddChild(_enabled);
        Content.AddChild(VectorRow("Position", _position, v => _node.Position = v));
        Content.AddChild(VectorRow("Rotation", _rotation, v => _node.Rotation = v));
        Content.AddChild(VectorRow("Scale", _scale, v => _node.Scale = v));
        Content.AddChild(new Label { Text = "Components", FontColor = DarkTheme.Accent });
        Content.AddChild(_components);

        _enabled.Toggled += (_, on) =>
        {
            if (_node != null) _node.Enabled = on;
        };
        Select(null);
    }

    public void Select(Node3D node)
    {
        _node = node;
        _shownComponents.Clear();
        _components.ClearChildren();
        Refresh();
    }

    public override void Refresh()
    {
        bool has = _node != null;
        _enabled.Visible = has;
        foreach (var edit in _position) edit.Editable = has;
        foreach (var edit in _rotation) edit.Editable = has;
        foreach (var edit in _scale) edit.Editable = has;
        if (!has)
        {
            _name.Text = "Select a node in the hierarchy";
            return;
        }

        _name.Text = $"{_node.Name} ({_node.GetType().Name})";
        if (_enabled.ButtonPressed != _node.Enabled) _enabled.ButtonPressed = _node.Enabled;
        Show(_position, _node.Position);
        Show(_rotation, _node.Rotation);
        Show(_scale, _node.Scale);
        RefreshComponents();
    }

    private void RefreshComponents()
    {
        bool same = _shownComponents.Count == _node.Components.Count;
        for (int i = 0; same && i < _shownComponents.Count; i++) same = ReferenceEquals(_shownComponents[i], _node.Components[i]);
        if (same) return;

        _shownComponents.Clear();
        _components.ClearChildren();
        foreach (var component in _node.Components)
        {
            var check = new CheckBox(component.GetType().Name, component.Enabled);
            var target = component;
            check.Toggled += (_, on) => target.Enabled = on;
            _components.AddChild(check);
            _shownComponents.Add(component);
        }
    }

    private static NumberEdit[] CreateVector(double step)
    {
        var edits = new NumberEdit[3];
        for (int i = 0; i < 3; i++) edits[i] = new NumberEdit { Step = step, Decimals = 2, SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
        return edits;
    }

    private HBoxContainer VectorRow(string label, NumberEdit[] edits, Action<Vector3> apply)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(64f, 0f) });
        foreach (var edit in edits)
        {
            edit.CustomMinimumSize = new Vector2(48f, 0f);
            edit.ValueChanged += _ =>
            {
                if (_node != null) apply(new Vector3((float)edits[0].Value, (float)edits[1].Value, (float)edits[2].Value));
            };
            row.AddChild(edit);
        }
        return row;
    }

    private static void Show(NumberEdit[] edits, Vector3 value)
    {
        if (edits[0].HasFocus || edits[1].HasFocus || edits[2].HasFocus) return;
        edits[0].Value = value.X;
        edits[1].Value = value.Y;
        edits[2].Value = value.Z;
    }
}
