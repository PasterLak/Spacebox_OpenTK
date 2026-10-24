using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.UI;

public abstract class ParticleRow : HBoxContainer
{
    private const float LabelWidth = 96f;

    protected ParticleRow(string label)
    {
        SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;
        AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(LabelWidth, 0f), Overrun = TextOverrun.Ellipsis, TooltipText = label, MouseFilter = MouseFilter.Pass });
    }

    public abstract void Refresh();

    protected static NumberEdit Number(double step, bool integer = false)
    {
        return new NumberEdit { Step = step, Decimals = integer ? 0 : 2, Integer = integer, CustomMinimumSize = new Vector2(48f, 0f), SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
    }

    protected static void Show(NumberEdit edit, double value)
    {
        if (!edit.HasFocus) edit.Value = value;
    }
}

public sealed class ParticleNumberRow : ParticleRow
{
    private readonly NumberEdit _edit;
    private readonly Func<float> _get;

    public ParticleNumberRow(string label, Func<float> get, Action<float> set, double step = 0.1, bool integer = false) : base(label)
    {
        _get = get;
        _edit = AddChild(Number(step, integer));
        _edit.ValueChanged += value => set((float)value);
    }

    public override void Refresh() => Show(_edit, _get());
}

public sealed class ParticleCheckRow : ParticleRow
{
    private readonly CheckBox _check = new CheckBox();
    private readonly Func<bool> _get;

    public ParticleCheckRow(string label, Func<bool> get, Action<bool> set) : base(label)
    {
        _get = get;
        AddChild(_check);
        _check.Toggled += (_, on) => set(on);
    }

    public override void Refresh()
    {
        bool value = _get();
        if (_check.ButtonPressed != value) _check.ButtonPressed = value;
    }
}

public sealed class ParticleVectorRow : ParticleRow
{
    private readonly NumberEdit[] _edits = new NumberEdit[3];
    private readonly Func<Vector3> _get;

    public ParticleVectorRow(string label, Func<Vector3> get, Action<Vector3> set, double step = 0.1) : base(label)
    {
        _get = get;
        for (int i = 0; i < 3; i++)
        {
            _edits[i] = AddChild(Number(step));
            _edits[i].ValueChanged += _ => set(new Vector3((float)_edits[0].Value, (float)_edits[1].Value, (float)_edits[2].Value));
        }
    }

    public override void Refresh()
    {
        var value = _get();
        Show(_edits[0], value.X);
        Show(_edits[1], value.Y);
        Show(_edits[2], value.Z);
    }
}

public sealed class ParticleRangeRow : ParticleRow
{
    private readonly NumberEdit _min;
    private readonly NumberEdit _max;
    private readonly Func<Vector2> _get;

    public ParticleRangeRow(string label, Func<Vector2> get, Action<float, float> set) : base(label)
    {
        _get = get;
        _min = AddChild(Number(0.1));
        _max = AddChild(Number(0.1));
        _min.TooltipText = "Min";
        _max.TooltipText = "Max";
        _min.ValueChanged += _ => set((float)_min.Value, (float)_max.Value);
        _max.ValueChanged += _ => set((float)_min.Value, (float)_max.Value);
    }

    public override void Refresh()
    {
        var value = _get();
        Show(_min, value.X);
        Show(_max, value.Y);
    }
}

public sealed class ParticleColorRow : ParticleRow
{
    private readonly ColorPickerButton _picker = new ColorPickerButton { EditAlpha = true, SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
    private readonly Func<Vector4> _get;

    public ParticleColorRow(string label, Func<Vector4> get, Action<Vector4> set) : base(label)
    {
        _get = get;
        AddChild(_picker);
        _picker.ColorChanged += color => set(new Vector4(color.R, color.G, color.B, color.A));
    }

    public override void Refresh()
    {
        var value = _get();
        var color = new Color4(value.X, value.Y, value.Z, value.W);
        if (_picker.Color != color) _picker.Color = color;
    }
}

public sealed class ParticleChoiceRow : ParticleRow
{
    private readonly ComboBox _combo = new ComboBox { Editable = false, SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
    private readonly IReadOnlyList<string> _options;
    private readonly Func<int> _get;
    private bool _refreshing;

    public ParticleChoiceRow(string label, IReadOnlyList<string> options, Func<int> get, Action<int> set) : base(label)
    {
        _options = options;
        _get = get;
        foreach (var option in options) _combo.AddOption(option);
        AddChild(_combo);
        _combo.ValueChanged += value =>
        {
            if (_refreshing) return;
            for (int i = 0; i < _options.Count; i++)
            {
                if (_options[i] == value) set(i);
            }
        };
    }

    public override void Refresh()
    {
        int index = _get();
        string value = index >= 0 && index < _options.Count ? _options[index] : "";
        if (_combo.IsOpen || _combo.Value == value) return;
        _refreshing = true;
        _combo.Value = value;
        _refreshing = false;
    }
}
