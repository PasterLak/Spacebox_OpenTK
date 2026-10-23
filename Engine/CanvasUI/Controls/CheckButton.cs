using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class CheckButton : CheckBox
{
    public CheckButton()
    {
    }

    public CheckButton(string text, bool pressed = false) : base(text, pressed)
    {
    }

    protected override float IndicatorWidth => MathF.Round(BoxSize * GetThemeConstant("switch_width_scale", 2f));

    protected override void DrawIndicator(ICanvasRenderer renderer, Rect2 trackRect)
    {
        DrawBox(renderer, trackRect);

        float inset = MathF.Max(2f, MathF.Floor(trackRect.Height * 0.15f));
        float knob = trackRect.Height - inset * 2f;
        float x = ButtonPressed ? trackRect.X + trackRect.Width - inset - knob : trackRect.X + inset;
        if (ButtonPressed) renderer.DrawRect(new Rect2(trackRect.X + inset, trackRect.Y + inset, x - trackRect.X - inset, knob), WithAlpha(CheckColor, 0.35f));

        var color = ButtonPressed ? CheckColor : GetThemeColor("switch_off_color", new Color4(0.55f, 0.57f, 0.6f, 1f));
        renderer.DrawRect(new Rect2(x, trackRect.Y + inset, knob, knob), Disabled ? WithAlpha(color, 0.5f) : color);
    }

    private static Color4 WithAlpha(Color4 color, float alpha) => new Color4(color.R, color.G, color.B, color.A * alpha);
}
