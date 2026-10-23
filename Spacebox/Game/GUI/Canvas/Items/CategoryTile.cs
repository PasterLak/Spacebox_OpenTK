using Engine.CanvasUI;
using Engine.Graphics.Atlas;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class CategoryTile : Control
{
    private const float IconPaddingRatio = 0.14f;

    private readonly AtlasSprite _icon;
    private readonly string _caption;

    public CategoryTile(AtlasSprite icon, string caption, string tooltip, float size = ItemSlotControl.DefaultSize)
    {
        _icon = icon;
        _caption = caption ?? "";
        TooltipText = tooltip;
        ThemeTypeVariation = "TileButton";
        MouseFilter = MouseFilter.Stop;
        CustomMinimumSize = new Vector2(size, size);
    }

    public bool Selected { get; set; }
    public event Action Clicked;
    public override bool IsInteractive => true;

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left) return false;
        Clicked?.Invoke();
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var style = GetThemeStyleBox(Selected ? "selected" : IsHovered ? "hover" : "normal") ?? GetThemeStyleBox("normal");
        style?.Draw(renderer, LocalRect);

        float padding = MathF.Round(Size.X * IconPaddingRatio);
        if (_icon.IsValid)
        {
            AtlasSpriteDraw.Draw(renderer, _icon, LocalRect.Shrink(padding, padding, padding, padding), Color4.White);
            return;
        }

        var font = GetThemeFont();
        if (font == null || _caption.Length == 0) return;
        float size = GetThemeFontSize();
        var lines = _caption.Split('\n');
        float lineHeight = font.GetLineHeight(size);
        float y = MathF.Round((Size.Y - lines.Length * lineHeight) * 0.5f);
        foreach (var line in lines)
        {
            float width = font.Measure(line, size).X;
            CanvasDraw.Text(renderer, font, new Vector2(MathF.Round((Size.X - width) * 0.5f), y), line, size, GetThemeColor("slot_count_color", Color4.White));
            y += lineHeight;
        }
    }
}
