using Engine.CanvasUI;
using Engine.Graphics.Atlas;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class ItemIconControl : Control
{
    private static readonly Color4 DimmedTint = new Color4(0.9f, 0.9f, 0.9f, 0.25f);

    private AtlasSprite _icon;
    private int _count;
    private string _countText = "";

    public ItemIconControl(float size)
    {
        CustomMinimumSize = new Vector2(size, size);
        MouseFilter = MouseFilter.Stop;
    }

    public bool Dimmed { get; set; }

    public void Set(AtlasSprite icon, int count, string title = "", string details = "")
    {
        _icon = icon;
        if (count != _count)
        {
            _count = count;
            _countText = count > 1 ? count.ToString() : "";
        }
        TooltipTitle = title ?? "";
        TooltipText = details ?? "";
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        AtlasSpriteDraw.Draw(renderer, _icon, LocalRect, Dimmed ? DimmedTint : Color4.White);
        if (_countText.Length == 0) return;

        var font = GetThemeFont();
        if (font == null) return;
        float size = GetThemeFontSize();
        var textSize = font.Measure(_countText, size);
        var position = new Vector2(Size.X - textSize.X, Size.Y - textSize.Y);
        CanvasDraw.Text(renderer, font, position + Vector2.One, _countText, size, GetThemeColor("font_shadow_color", Color4.Black));
        CanvasDraw.Text(renderer, font, position, _countText, size, GetThemeColor("slot_count_color", Color4.White));
    }
}
