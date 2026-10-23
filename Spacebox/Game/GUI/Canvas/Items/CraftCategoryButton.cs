using Engine.CanvasUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class CraftCategoryButton : Control
{
    public static readonly Vector2 DefaultSize = new Vector2(72f, 54f);
    private const float IconSize = 26f;

    private readonly CraftingCategory _category;

    public CraftCategoryButton(CraftingCategory category)
    {
        _category = category;
        ThemeTypeVariation = "Slot";
        MouseFilter = MouseFilter.Stop;
        CustomMinimumSize = DefaultSize;
        TooltipTitle = category.Name;
        TooltipText = $"{category.ItemsCountInCategory} items";
    }

    public CraftingCategory Category => _category;
    public event Action<CraftingCategory> Clicked;
    public override bool IsInteractive => true;

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left) return false;
        Clicked?.Invoke(_category);
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var style = GetThemeStyleBox(IsHovered ? "hover" : "normal") ?? GetThemeStyleBox("normal");
        style?.Draw(renderer, LocalRect);
        AtlasSpriteDraw.Draw(renderer, _category.IconSprite, new Rect2(Size.X - IconSize - 6f, Size.Y - IconSize - 4f, IconSize, IconSize), Color4.White);

        var font = GetThemeFont();
        if (font == null) return;
        CanvasDraw.Text(renderer, font, new Vector2(6f, 4f), _category.Name, GetThemeFontSize(), GetThemeColor("slot_count_color", Color4.White));
    }
}
