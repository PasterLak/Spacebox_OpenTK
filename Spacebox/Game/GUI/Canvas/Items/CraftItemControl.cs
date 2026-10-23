using Engine.CanvasUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class CraftItemControl : Control
{
    private const float IconPaddingRatio = 0.14f;
    private static readonly Color4 DimmedTint = new Color4(0.9f, 0.9f, 0.9f, 0.25f);

    private readonly CraftingCategory.Data _data;
    private bool _canCraft;
    private int _count = -1;
    private string _countText = "";

    public CraftItemControl(CraftingCategory.Data data, float size = ItemSlotControl.DefaultSize)
    {
        _data = data;
        ThemeTypeVariation = "Slot";
        MouseFilter = MouseFilter.Stop;
        CustomMinimumSize = new Vector2(size, size);
        TooltipTitle = data?.item?.Name ?? "";
    }

    public CraftingCategory.Data Data => _data;
    public event Action<CraftItemControl> Hovered;
    public event Action<CraftItemControl> Clicked;
    public override bool IsInteractive => true;

    public void Refresh()
    {
        _canCraft = CraftingGUI.CanCraft(_data);
        int count = _canCraft ? CraftingGUI.PossibleCount(_data) : 0;
        if (count == _count) return;
        _count = count;
        _countText = count > 1 ? count.ToString() : "";
    }

    protected override void OnMouseEnter()
    {
        base.OnMouseEnter();
        Hovered?.Invoke(this);
    }

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left) return false;
        Clicked?.Invoke(this);
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var style = GetThemeStyleBox(IsHovered ? "hover" : "normal") ?? GetThemeStyleBox("normal");
        style?.Draw(renderer, LocalRect);
        if (_data?.item == null) return;

        float padding = MathF.Round(Size.X * IconPaddingRatio);
        AtlasSpriteDraw.Draw(renderer, _data.item.Icon, LocalRect.Shrink(padding, padding, padding, padding), _canCraft || IsHovered ? Color4.White : DimmedTint);
        if (_countText.Length == 0) return;

        var font = GetThemeFont();
        if (font == null) return;
        float size = GetThemeFontSize();
        var textSize = font.Measure(_countText, size);
        var position = new Vector2(padding * 0.5f, Size.Y - textSize.Y - padding * 0.3f);
        CanvasDraw.Text(renderer, font, position + Vector2.One, _countText, size, GetThemeColor("font_shadow_color", Color4.Black));
        CanvasDraw.Text(renderer, font, position, _countText, size, GetThemeColor("slot_count_color", Color4.White));
    }
}
