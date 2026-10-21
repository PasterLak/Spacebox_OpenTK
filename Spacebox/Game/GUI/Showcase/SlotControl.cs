using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Game.GUI.Showcase;

public sealed class SlotControl : Control
{
    private readonly ShowcaseSlot _slot;
    private readonly Texture2D _placeholder;
    private int _count = -1;
    private string _countText = "";

    public bool Selected { get; set; }
    public float IconPadding { get; set; } = 6f;
    public event Action<SlotControl> Clicked;

    public SlotControl(ShowcaseSlot slot, float size, Texture2D placeholder = null)
    {
        _slot = slot;
        _placeholder = placeholder;
        CustomMinimumSize = new Vector2(size, size);
        ThemeTypeVariation = "Slot";
        TooltipText = slot?.Item?.Name ?? "";
    }

    public string Caption { get; set; }

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left) return false;
        Clicked?.Invoke(this);
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var style = GetThemeStyleBox(Selected ? "selected" : IsHovered ? "hover" : "normal") ?? GetThemeStyleBox("normal");
        style?.Draw(renderer, LocalRect);

        var icon = _slot?.Item?.Icon;
        var inner = LocalRect.Shrink(IconPadding, IconPadding, IconPadding, IconPadding);
        if (icon != null)
        {
            renderer.DrawTexture(icon, inner, CanvasDraw.UvOf(icon, null), Color4.White);
        }
        else if (_placeholder != null)
        {
            renderer.DrawTexture(_placeholder, inner, CanvasDraw.UvOf(_placeholder, null), GetThemeColor("placeholder_color", new Color4(1f, 1f, 1f, 0.25f)));
        }

        var font = GetThemeFont();
        if (font == null) return;
        float size = GetThemeFontSize();

        if (_slot != null && _slot.Item != null && _slot.Count > 1)
        {
            string text = CountText(_slot.Count);
            var textSize = font.Measure(text, size);
            var position = new Vector2(Size.X - textSize.X - 3f, Size.Y - textSize.Y - 1f);
            CanvasDraw.Text(renderer, font, position + Vector2.One, text, size, GetThemeColor("count_shadow", new Color4(0f, 0f, 0f, 0.8f)));
            CanvasDraw.Text(renderer, font, position, text, size, GetThemeColor("count_color", Color4.White));
        }

        if (!string.IsNullOrEmpty(Caption))
            CanvasDraw.Text(renderer, font, new Vector2(3f, 1f), Caption, size, GetThemeColor("caption_color", new Color4(1f, 1f, 1f, 0.6f)));
    }

    private string CountText(int count)
    {
        if (count != _count)
        {
            _count = count;
            _countText = count.ToString();
        }
        return _countText;
    }
}
