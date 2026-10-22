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

    public override Vector2 GetMinimumSize()
    {
        var font = GetThemeFont();
        var text = font == null || _text.Length == 0 ? Vector2.Zero : font.Measure(_text, GetThemeFontSize());
        float gap = text.X > 0f ? GetThemeConstant("h_separation", 6f) : 0f;
        return new Vector2(BoxSize + gap + text.X, MathF.Max(BoxSize, text.Y));
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        float box = BoxSize;
        var boxRect = new Rect2(0f, MathF.Floor((Size.Y - box) * 0.5f), box, box);
        var style = GetThemeStyleBox(IsHovered ? "box_hover" : "box") ?? GetThemeStyleBox("box");
        if (style != null) style.Draw(renderer, boxRect);
        else renderer.DrawRect(boxRect, new Color4(0.15f, 0.17f, 0.2f, 1f));

        if (ButtonPressed)
        {
            float inset = MathF.Max(3f, box * 0.25f);
            renderer.DrawRect(boxRect.Shrink(inset, inset, inset, inset), GetThemeColor("check_color", new Color4(1f, 0.77f, 0.16f, 1f)));
        }

        if (_text.Length == 0) return;
        var font = GetThemeFont();
        var textSize = font.Measure(_text, GetThemeFontSize());
        var color = Disabled ? GetThemeColor("font_disabled_color", Color4.Gray) : GetThemeColor("font_color", Color4.White);
        CanvasDraw.Text(renderer, font, new Vector2(box + GetThemeConstant("h_separation", 6f), MathF.Floor((Size.Y - textSize.Y) * 0.5f)), _text, GetThemeFontSize(), color);
    }
}
