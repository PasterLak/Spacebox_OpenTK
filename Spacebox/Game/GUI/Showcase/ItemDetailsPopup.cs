using Engine.CanvasUI;
using OpenTK.Mathematics;
using static Spacebox.Game.GUI.Showcase.ShowcaseParts;

namespace Spacebox.Game.GUI.Showcase;

public sealed class ItemDetailsPopup : PopupPanel
{
    private readonly Label _title;
    private readonly Label _count;
    private readonly Label _hint;

    public SlotControl Target { get; private set; }
    public Button CloseButton { get; }

    public ItemDetailsPopup(Color4 titleColor, Color4 textColor, Color4 hintColor)
    {
        Name = "ItemDetails";
        ThemeTypeVariation = "Popup";
        Visible = false;
        Gap = 6f;
        CustomMaximumSize = new Vector2(320f, 0f);

        var column = AddChild(new VBoxContainer());
        column.AddThemeConstantOverride("separation", 6f);
        _title = column.AddChild(Text("", titleColor, 24f));
        _title.Overrun = TextOverrun.Ellipsis;
        _title.CustomMinimumSize = new Vector2(200f, 0f);
        _count = column.AddChild(Text("", textColor, 12f));
        _hint = column.AddChild(Text("Shift + click moves the whole stack", hintColor, 12f));
        _hint.Overrun = TextOverrun.Ellipsis;

        var buttons = column.AddChild(new HBoxContainer());
        buttons.AddThemeConstantOverride("separation", 6f);
        buttons.AddChild(new Button("Use") { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        buttons.AddChild(new Button("Drop") { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        CloseButton = buttons.AddChild(new Button("Close") { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        CloseButton.Pressed += _ => Hide();
    }

    public void Show(SlotControl slot)
    {
        Target = slot;
        var item = slot.Slot?.Item;
        _title.Text = item?.Name ?? "Empty slot";
        _count.Text = item == null ? "Nothing stored here" : $"Stack: {slot.Slot.Count} / 99";
        Popup(slot.Rect, PopupSide.Right);
    }

    public void Hide()
    {
        Visible = false;
        Target = null;
    }
}
