using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class CheckBox : BaseButton
{
    private string _text = "";

    public CheckBox()
    {
        ToggleMode = true;
        FocusMode = FocusMode.Click;
    }

    public CheckBox(string text, bool pressed = false) : this()
    {
        _text = text ?? "";
        ButtonPressed = pressed;
    }

    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? "";
            QueueLayout();
        }
    }

    public float BoxSize => GetThemeConstant("box_size", 14f);
    public Texture2D Icon { get; set; }
    public Rect2? IconRegion { get; set; }
    public float IconSize { get; set; } = 16f;

    protected virtual float IndicatorWidth => BoxSize;

    private float IconWidth => Icon == null ? 0f : IconSize + GetThemeConstant("h_separation", 6f);

    public override Vector2 GetMinimumSize()
    {
        var font = GetThemeFont();
        var text = font == null || _text.Length == 0 ? Vector2.Zero : font.Measure(_text, GetThemeFontSize());
        float gap = text.X > 0f || Icon != null ? GetThemeConstant("h_separation", 6f) : 0f;
        return new Vector2(IndicatorWidth + gap + IconWidth + text.X, MathF.Max(MathF.Max(BoxSize, text.Y), Icon != null ? IconSize : 0f));
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        float box = BoxSize;
        DrawIndicator(renderer, new Rect2(0f, MathF.Floor((Size.Y - box) * 0.5f), IndicatorWidth, box));

        float x = IndicatorWidth + GetThemeConstant("h_separation", 6f);
        if (Icon != null)
        {
            var tint = Disabled ? new Color4(1f, 1f, 1f, 0.5f) : Color4.White;
            renderer.DrawTexture(Icon, new Rect2(x, MathF.Floor((Size.Y - IconSize) * 0.5f), IconSize, IconSize), CanvasDraw.UvOf(Icon, IconRegion), tint);
            x += IconWidth;
        }

        if (_text.Length == 0) return;
        var font = GetThemeFont();
        var textSize = font.Measure(_text, GetThemeFontSize());
        var color = Disabled ? GetThemeColor("font_disabled_color", Color4.Gray) : GetThemeColor("font_color", Color4.White);
        CanvasDraw.Text(renderer, font, new Vector2(x, MathF.Floor((Size.Y - textSize.Y) * 0.5f)), _text, GetThemeFontSize(), color);
    }

    protected virtual void DrawIndicator(ICanvasRenderer renderer, Rect2 boxRect)
    {
        DrawBox(renderer, boxRect);

        if (ButtonPressed)
        {
            float inset = MathF.Max(3f, boxRect.Height * 0.25f);
            renderer.DrawRect(boxRect.Shrink(inset, inset, inset, inset), CheckColor);
        }
    }

    protected Color4 CheckColor => GetThemeColor("check_color", new Color4(1f, 0.77f, 0.16f, 1f));

    protected void DrawBox(ICanvasRenderer renderer, Rect2 rect)
    {
        var style = GetThemeStyleBox(IsHovered ? "box_hover" : "box") ?? GetThemeStyleBox("box");
        if (style != null) style.Draw(renderer, rect);
        else renderer.DrawRect(rect, new Color4(0.15f, 0.17f, 0.2f, 1f));
    }
}
