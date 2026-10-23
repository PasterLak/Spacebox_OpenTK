using Engine.CanvasUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Game.GUI.Canvas;

public sealed class HudSlotControl : Control
{
    private const float IconPaddingRatio = 0.14f;

    private ItemSlot _slot;
    private short _itemId = -1;
    private int _count = -1;
    private string _countText = "";

    public HudSlotControl()
    {
        ThemeTypeVariation = "Slot";
        MouseFilter = MouseFilter.Stop;
    }

    public bool Selected { get; set; }
    public event Action<ItemSlot> Clicked;
    public override bool IsInteractive => true;

    public void Bind(ItemSlot slot)
    {
        _slot = slot;
        Refresh();
    }

    public void Refresh()
    {
        short id = _slot != null && _slot.HasItem ? _slot.Item.Id : (short)-1;
        int count = _slot != null && _slot.HasItem ? _slot.Count : 0;
        if (id == _itemId && count == _count) return;

        _itemId = id;
        _count = count;
        _countText = count > 1 ? count.ToString() : "";
        TooltipText = _slot != null && _slot.HasItem ? _slot.Item.Name : "";
    }

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left || _slot == null) return false;
        Clicked?.Invoke(_slot);
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var style = GetThemeStyleBox(Selected ? "selected" : IsHovered ? "hover" : "normal") ?? GetThemeStyleBox("normal");
        style?.Draw(renderer, LocalRect);
        if (_slot == null || !_slot.HasItem) return;

        float padding = MathF.Round(Size.X * IconPaddingRatio);
        AtlasSpriteDraw.Draw(renderer, _slot.Item.Icon, LocalRect.Shrink(padding, padding, padding, padding), Color4.White);
        if (_countText.Length == 0) return;

        var font = GetThemeFont();
        if (font == null) return;
        float size = GetThemeFontSize();
        var textSize = font.Measure(_countText, size);
        var position = new Vector2(Size.X - textSize.X - padding * 0.5f, Size.Y - textSize.Y - padding * 0.3f);
        CanvasDraw.Text(renderer, font, position + Vector2.One, _countText, size, GetThemeColor("font_shadow_color", Color4.Black));
        CanvasDraw.Text(renderer, font, position, _countText, size, GetThemeColor("slot_count_color", Color4.White));
    }
}
