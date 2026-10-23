using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class ParallaxTitle : Control
{
    private readonly float _fontSize;
    private readonly Color4 _color;
    private readonly Color4 _nearShadow;
    private readonly Color4 _farShadow;
    private readonly float _shadowStep;
    private string _text;
    private Vector2 _offset;

    public ParallaxTitle(string text, float fontSize, Color4 color, Color4 nearShadow, Color4 farShadow, float shadowStep)
    {
        Name = "ParallaxTitle";
        MouseFilter = MouseFilter.Ignore;
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _text = text ?? "";
        _fontSize = fontSize;
        _color = color;
        _nearShadow = nearShadow;
        _farShadow = farShadow;
        _shadowStep = shadowStep;
    }

    public float Intensity { get; set; } = 0.02f;

    public string Text
    {
        get => _text;
        set
        {
            value ??= "";
            if (_text == value) return;
            _text = value;
            QueueLayout();
        }
    }

    public void Tick()
    {
        var root = Root;
        var screen = root?.ScreenSize ?? Vector2.Zero;
        if (screen.X <= 0f || screen.Y <= 0f) return;
        var mouse = Input.Mouse.Position;
        _offset = (new Vector2(mouse.X / screen.X, mouse.Y / screen.Y) - new Vector2(0.5f)) * root.ViewportSize * Intensity;
    }

    public override Vector2 GetMinimumSize()
    {
        var font = GetThemeFont();
        if (font == null || _text.Length == 0) return new Vector2(0f, _fontSize);
        return font.Measure(_text, _fontSize) + new Vector2(_shadowStep * 2f);
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var font = GetThemeFont();
        if (font == null || _text.Length == 0) return;
        var size = font.Measure(_text, _fontSize);
        var origin = new Vector2(MathF.Round((Size.X - size.X) * 0.5f + _offset.X), MathF.Round(_offset.Y));
        if (_farShadow.A > 0f) CanvasDraw.Text(renderer, font, origin + new Vector2(_shadowStep * 2f), _text, _fontSize, _farShadow);
        if (_nearShadow.A > 0f) CanvasDraw.Text(renderer, font, origin + new Vector2(_shadowStep), _text, _fontSize, _nearShadow);
        CanvasDraw.Text(renderer, font, origin, _text, _fontSize, _color);
    }
}
