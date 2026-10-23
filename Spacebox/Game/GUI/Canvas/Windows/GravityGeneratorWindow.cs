using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class GravityGeneratorWindow : GameWindow
{
    private const float ContentWidth = 300f;

    private readonly CheckBox _enabled = new CheckBox("Gravity");
    private readonly CheckBox _insideOnly = new CheckBox("Only inside closed rooms");
    private readonly Label _radiusLabel = new Label();
    private readonly HSlider _radius = new HSlider { Step = 1, CustomMinimumSize = new Vector2(ContentWidth, 0f) };
    private readonly Label _strengthLabel = new Label();
    private readonly HSlider _strength = new HSlider { Step = 0.01, CustomMinimumSize = new Vector2(ContentWidth, 0f) };
    private readonly HBoxContainer _presets = new HBoxContainer();
    private readonly List<(Button Button, float Strength)> _presetButtons = new List<(Button, float)>();
    private readonly Label _power = new Label();
    private readonly Label _status = StatusText.Create();
    private GravityGeneratorBlock _shown;
    private bool _syncing;

    public GravityGeneratorWindow() : base("Gravity Generator")
    {
        Name = "GravityGenerator";
        Body.AddChild(_enabled);
        Body.AddChild(_insideOnly);
        Body.AddChild(_radiusLabel);
        Body.AddChild(_radius);
        Body.AddChild(_strengthLabel);
        Body.AddChild(_strength);
        Body.AddChild(_presets);
        Body.AddChild(_power);
        Body.AddChild(_status);

        _enabled.Toggled += (_, _) => Send();
        _insideOnly.Toggled += (_, _) => Send();
        _radius.ValueChanged += _ => Send();
        _strength.ValueChanged += _ => Send();
        _radius.DragEnded += GravityGeneratorUI.ApplyPendingPower;
        _strength.DragEnded += GravityGeneratorUI.ApplyPendingPower;
    }

    public override bool IsOpen => GravityGeneratorUI.IsOpen;

    protected override void Refresh()
    {
        var block = GravityGeneratorUI.Block;
        if (TitleLabel.Text != GravityGeneratorUI.BlockName) TitleLabel.Text = GravityGeneratorUI.BlockName;
        if (!ReferenceEquals(block, _shown)) Bind(block);

        _syncing = true;
        if (!_radius.HasFocus && !Input.IsMouseButton(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left))
        {
            _radius.Value = block.RadiusSetting;
            _strength.Value = block.Strength;
            _enabled.ButtonPressed = block.IsGravityEnabled;
            _insideOnly.ButtonPressed = block.InsideOnly;
        }
        _syncing = false;

        SetText(_radiusLabel, $"Radius: {(int)_radius.Value} blocks");
        SetText(_strengthLabel, $"Gravity: {_strength.Value:0.00} m/s2");
        foreach (var (button, strength) in _presetButtons)
            button.ThemeTypeVariation = MathF.Abs((float)_strength.Value - strength) < GravityGeneratorUI.PresetTolerance ? "" : "SmallButton";
        SetText(_power, $"Power needed: {block.RequiredPower} EU");
        var (text, kind) = GravityGeneratorUI.Status();
        StatusText.Set(_status, text, kind);
    }

    private void Bind(GravityGeneratorBlock block)
    {
        _shown = block;
        var data = block.Data;
        _radius.MinValue = data.MinRadius;
        _radius.MaxValue = data.MaxRadius;
        _strength.MinValue = data.MinStrength;
        _strength.MaxValue = data.MaxStrength;

        _presets.ClearChildren();
        _presetButtons.Clear();
        foreach (var preset in data.Presets)
        {
            float strength = preset.Strength;
            var button = _presets.AddChild(new Button { Text = preset.Name, ThemeTypeVariation = "SmallButton", SizeFlagsHorizontal = SizeFlags.ExpandFill });
            button.Pressed += _ =>
            {
                _strength.Value = strength;
                Send();
                GravityGeneratorUI.ApplyPendingPower();
            };
            _presetButtons.Add((button, strength));
        }
    }

    private void Send()
    {
        if (_syncing) return;
        GravityGeneratorUI.Configure((int)Math.Round(_radius.Value), (float)_strength.Value, _enabled.ButtonPressed, _insideOnly.ButtonPressed);
        if (!Input.IsMouseButton(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left)) GravityGeneratorUI.ApplyPendingPower();
    }

    private static void SetText(Label label, string text)
    {
        if (label.Text != text) label.Text = text;
    }
}
