using Engine.CanvasUI;
using Engine.Graphics.Atlas;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class ItemSlotControl : Control, ISlotDropTarget
{
    public const float DefaultSize = 43f;
    private const float IconPaddingRatio = 0.14f;
    private const float DragThreshold = 4f;
    private const string CreativeMark = "c";
    private static readonly Color4 CreativeColor = new Color4(1f, 0.8f, 0f, 1f);
    private static readonly Color4 BackgroundTint = new Color4(1f, 1f, 1f, 0.35f);
    private static readonly Color4 DraggedTint = new Color4(1f, 1f, 1f, 0.35f);

    private ItemSlot _slot;
    private AtlasSprite _background;
    private short _itemId = -2;
    private int _count = -1;
    private string _hint;
    private string _countText = "";
    private bool _pressing;
    private Vector2 _pressPosition;

    public ItemSlotControl(float size = DefaultSize)
    {
        ThemeTypeVariation = "Slot";
        MouseFilter = MouseFilter.Stop;
        CustomMinimumSize = new Vector2(size, size);
    }

    public ItemSlot Slot => _slot;
    public bool Selected { get; set; }
    public bool AllowsDrag { get; set; } = true;
    public bool AllowsSplit { get; set; } = true;
    public bool AcceptsDrops { get; set; } = true;
    public bool ShowsStackSize { get; set; }
    public bool ShowsDescription { get; set; } = true;
    public bool ShowsId { get; set; }
    public event Action<ItemSlot> Clicked;
    public event Action<ItemSlot> Hovered;
    public override bool IsInteractive => true;

    public void Bind(ItemSlot slot, AtlasSprite background = default)
    {
        _slot = slot;
        _background = background;
        _itemId = -2;
        Refresh();
    }

    public void Refresh()
    {
        short id = _slot != null && _slot.HasItem ? _slot.Item.Id : (short)-1;
        int count = _slot != null && _slot.HasItem ? _slot.Count : 0;
        string hint = _slot?.Hint ?? "";
        if (id == _itemId && count == _count && hint == _hint) return;

        _itemId = id;
        _count = count;
        _hint = hint;
        _countText = count > 1 ? count.ToString() : "";
        TooltipText = BuildTooltip();
    }

    public void AcceptDrop(ItemSlot source)
    {
        if (AcceptsDrops && _slot != null) SlotTransfer.Move(source, _slot);
    }

    protected override void OnMouseEnter()
    {
        base.OnMouseEnter();
        if (_slot != null) Hovered?.Invoke(_slot);
    }

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (_slot == null) return false;
        if (button == MouseButton.Right)
        {
            if (AllowsSplit && _slot.HasItem)
            {
                _slot.Split();
                InventoryUI.splitAudio?.Play();
            }
            return true;
        }
        if (button != MouseButton.Left) return false;

        _pressing = true;
        _pressPosition = localPosition;
        return true;
    }

    protected override void OnMouseMove(Vector2 localPosition, bool pressed)
    {
        base.OnMouseMove(localPosition, pressed);
        if (!_pressing || !pressed || !AllowsDrag || _slot == null || !_slot.HasItem) return;
        if ((localPosition - _pressPosition).Length < DragThreshold) return;

        _pressing = false;
        SlotDrag.Begin(_slot);
    }

    protected override bool OnMouseUp(Vector2 localPosition, MouseButton button, bool inside)
    {
        if (button != MouseButton.Left) return false;

        if (SlotDrag.IsActive && ReferenceEquals(SlotDrag.Source, _slot))
        {
            SlotDrag.Release(Root);
        }
        else if (_pressing && inside && _slot != null)
        {
            Clicked?.Invoke(_slot);
        }

        _pressing = false;
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var style = GetThemeStyleBox(Selected ? "selected" : IsHovered ? "hover" : "normal") ?? GetThemeStyleBox("normal");
        style?.Draw(renderer, LocalRect);
        if (_slot == null) return;

        float padding = MathF.Round(Size.X * IconPaddingRatio);
        var inner = LocalRect.Shrink(padding, padding, padding, padding);
        if (!_slot.HasItem)
        {
            AtlasSpriteDraw.Draw(renderer, _background, inner, BackgroundTint);
            return;
        }

        bool dragged = ReferenceEquals(SlotDrag.Source, _slot);
        AtlasSpriteDraw.Draw(renderer, _slot.Item.Icon, inner, dragged ? DraggedTint : Color4.White);

        var font = GetThemeFont();
        if (font == null) return;
        float size = GetThemeFontSize();
        var shadow = GetThemeColor("font_shadow_color", Color4.Black);

        if (_countText.Length > 0)
        {
            var textSize = font.Measure(_countText, size);
            var position = new Vector2(Size.X - textSize.X - padding * 0.5f, Size.Y - textSize.Y - padding * 0.3f);
            CanvasDraw.Text(renderer, font, position + Vector2.One, _countText, size, shadow);
            CanvasDraw.Text(renderer, font, position, _countText, size, GetThemeColor("slot_count_color", Color4.White));
        }
        else if (_slot.Item is CreativeToolItem)
        {
            var position = new Vector2(padding * 0.5f, padding * 0.3f);
            CanvasDraw.Text(renderer, font, position + Vector2.One, CreativeMark, size, shadow);
            CanvasDraw.Text(renderer, font, position, CreativeMark, size, CreativeColor);
        }
    }

    private string BuildTooltip()
    {
        if (_slot == null) return "";
        if (!_slot.HasItem)
        {
            string name = _slot.Name ?? "";
            return string.IsNullOrEmpty(_hint) ? name : name + "\n" + _hint;
        }

        string text = _slot.Item.Name + ItemTooltipText.Details(_slot.Item, ShowsStackSize, ShowsDescription, ShowsId).TrimEnd();
        return string.IsNullOrEmpty(_hint) ? text : text + "\n" + _hint;
    }
}
