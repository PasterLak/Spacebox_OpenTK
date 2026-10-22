using System.Globalization;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public class ColorPickerButton : Control
{
    private readonly PopupPanel _popup = new PopupPanel { Name = "ColorPopup", Visible = false, AutoHide = true };
    private readonly HSlider[] _sliders = new HSlider[4];
    private readonly NumberEdit[] _numbers = new NumberEdit[4];
    private readonly LineEdit _hex = new LineEdit { PlaceholderText = "#RRGGBB" };
    private readonly HBoxContainer _alphaRow;
    private Color4 _color = Color4.White;
    private bool _syncing;

    public ColorPickerButton()
    {
        FocusMode = FocusMode.Click;
        _popup.Owner = this;
        var box = _popup.AddChild(new VBoxContainer());
        string[] names = { "R", "G", "B", "A" };
        for (int i = 0; i < 4; i++)
        {
            int channel = i;
            var row = box.AddChild(new HBoxContainer());
            row.AddChild(new Label(names[i]) { CustomMinimumSize = new Vector2(14f, 0f) });
            _sliders[i] = row.AddChild(new HSlider { MinValue = 0, MaxValue = 255, Step = 1, CustomMinimumSize = new Vector2(140f, 16f), SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand });
            _numbers[i] = row.AddChild(new NumberEdit { MinValue = 0, MaxValue = 255, Integer = true, CustomMinimumSize = new Vector2(56f, 0f) });
            _sliders[i].ValueChanged += value => SetChannel(channel, (float)value / 255f);
            _numbers[i].ValueChanged += value => SetChannel(channel, (float)value / 255f);
            if (i == 3) row.Visible = false;
        }
        _alphaRow = (HBoxContainer)_sliders[3].Parent;
        box.AddChild(_hex);
        _hex.TextSubmitted += ApplyHex;
        _hex.EditingFinished += ApplyHex;
    }

    public bool EditAlpha
    {
        get => _alphaRow.Visible;
        set => _alphaRow.Visible = value;
    }

    public override bool IsInteractive => true;

    public event Action<Color4> ColorChanged;

    public Color4 Color
    {
        get => _color;
        set
        {
            _color = value;
            Sync();
        }
    }

    public static string ToHex(Color4 color)
    {
        return "#" + Byte(color.R).ToString("X2") + Byte(color.G).ToString("X2") + Byte(color.B).ToString("X2");
    }

    public static bool TryParseHex(string text, out Color4 color)
    {
        color = Color4.White;
        text = (text ?? "").Trim().TrimStart('#');
        if (text.Length != 6 || !int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value)) return false;
        color = new Color4(((value >> 16) & 0xFF) / 255f, ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f, 1f);
        return true;
    }

    public override Vector2 GetMinimumSize() => new Vector2(40f, 18f);

    protected internal override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left || Root == null) return false;
        if (_popup.Visible && _popup.Parent != null)
        {
            Close();
            return true;
        }

        Sync();
        if (_popup.Parent == null) Root.Add(_popup, UiLayer.Popup);
        _popup.Popup(Rect, PopupSide.Below);
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        renderer.DrawRect(LocalRect, new Color4(0f, 0f, 0f, 0.6f));
        renderer.DrawRect(LocalRect.Shrink(2f, 2f, 2f, 2f), new Color4(_color.R, _color.G, _color.B, 1f));
        if (IsHovered) CanvasDraw.Outline(renderer, LocalRect, GetThemeColor("hover_border", new Color4(1f, 0.77f, 0.16f, 1f)), 1f, 1f, 1f, 1f);
    }

    private void Close()
    {
        _popup.Hide();
        _popup.Parent?.RemoveChild(_popup);
    }

    private void SetChannel(int channel, float value)
    {
        if (_syncing) return;
        _color = channel switch
        {
            0 => new Color4(value, _color.G, _color.B, _color.A),
            1 => new Color4(_color.R, value, _color.B, _color.A),
            2 => new Color4(_color.R, _color.G, value, _color.A),
            _ => new Color4(_color.R, _color.G, _color.B, value)
        };
        Sync();
        ColorChanged?.Invoke(_color);
    }

    private void ApplyHex(string text)
    {
        if (_syncing || !TryParseHex(text, out var parsed)) return;
        _color = new Color4(parsed.R, parsed.G, parsed.B, _color.A);
        Sync();
        ColorChanged?.Invoke(_color);
    }

    private void Sync()
    {
        _syncing = true;
        float[] channels = { _color.R, _color.G, _color.B, _color.A };
        for (int i = 0; i < 4; i++)
        {
            _sliders[i].Value = Byte(channels[i]);
            _numbers[i].Value = Byte(channels[i]);
        }
        if (!_hex.HasFocus) _hex.Text = ToHex(_color);
        _syncing = false;
    }

    private static int Byte(float value) => (int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f);
}
