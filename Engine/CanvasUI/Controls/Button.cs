using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public abstract class BaseButton : Control
{
    private bool _buttonPressed;

    public bool Disabled { get; set; }
    public bool ToggleMode { get; set; }
    public bool IsPressing { get; private set; }

    public event Action<BaseButton> Pressed;
    public event Action<BaseButton, bool> Toggled;

    public bool ButtonPressed
    {
        get => _buttonPressed;
        set
        {
            if (_buttonPressed == value) return;
            _buttonPressed = value;
            Toggled?.Invoke(this, value);
        }
    }

    public string DrawState
    {
        get
        {
            if (Disabled) return "disabled";
            if (IsPressing || (ToggleMode && _buttonPressed)) return "pressed";
            return IsHovered ? "hover" : "normal";
        }
    }

    public void EmitPressed()
    {
        if (Disabled) return;
        if (ToggleMode) ButtonPressed = !ButtonPressed;
        Pressed?.Invoke(this);
    }

    protected internal override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (Disabled || button != MouseButton.Left) return Disabled;
        IsPressing = true;
        return true;
    }

    protected internal override bool OnMouseUp(Vector2 localPosition, MouseButton button, bool inside)
    {
        if (button != MouseButton.Left || !IsPressing) return false;
        IsPressing = false;
        if (inside) EmitPressed();
        return true;
    }
}

public class Button : BaseButton
{
    private string _text = "";
    private Texture2D _icon;

    public Rect2? IconRegion { get; set; }
    public Vector2 IconSize { get; set; }
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Center;
    public float IconSeparation { get; set; } = 4f;

    public Button()
    {
    }

    public Button(string text)
    {
        _text = text ?? "";
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

    public Texture2D Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            QueueLayout();
        }
    }

    public StyleBox CurrentStyle => GetThemeStyleBox(DrawState) ?? GetThemeStyleBox("normal");

    public override Vector2 GetMinimumSize()
    {
        var content = ContentSize(out _, out _);
        var style = GetThemeStyleBox("normal");
        return content + (style?.MinimumSize ?? Vector2.Zero);
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var style = CurrentStyle;
        style?.Draw(renderer, LocalRect);

        var inner = style == null ? LocalRect : style.ContentRect(LocalRect);
        var content = ContentSize(out var textSize, out var iconSize);
        float x = Alignment == HorizontalAlignment.Center ? inner.X + (inner.Width - content.X) * 0.5f
            : Alignment == HorizontalAlignment.Right ? inner.End.X - content.X : inner.X;
        x = MathF.Floor(x);

        if (_icon != null)
        {
            float iy = MathF.Floor(inner.Y + (inner.Height - iconSize.Y) * 0.5f);
            renderer.DrawTexture(_icon, new Rect2(x, iy, iconSize.X, iconSize.Y), CanvasDraw.UvOf(_icon, IconRegion), Color4.White);
            x += iconSize.X + (textSize.X > 0f ? IconSeparation : 0f);
        }

        if (_text.Length == 0) return;
        var font = GetThemeFont();
        float ty = MathF.Floor(inner.Y + (inner.Height - textSize.Y) * 0.5f);
        var color = Disabled ? GetThemeColor("font_disabled_color", Color4.Gray)
            : IsHovered ? GetThemeColor("font_hover_color", GetThemeColor("font_color", Color4.White))
            : GetThemeColor("font_color", Color4.White);
        CanvasDraw.Text(renderer, font, new Vector2(x, ty), _text, GetThemeFontSize(), color);
    }

    private Vector2 ContentSize(out Vector2 textSize, out Vector2 iconSize)
    {
        var font = GetThemeFont();
        textSize = font == null || _text.Length == 0 ? Vector2.Zero : font.Measure(_text, GetThemeFontSize());
        iconSize = _icon == null ? Vector2.Zero : IconSize != Vector2.Zero ? IconSize : IconRegion?.Size ?? new Vector2(_icon.Width, _icon.Height);
        float gap = textSize.X > 0f && iconSize.X > 0f ? IconSeparation : 0f;
        return new Vector2(textSize.X + iconSize.X + gap, MathF.Max(textSize.Y, iconSize.Y));
    }
}
