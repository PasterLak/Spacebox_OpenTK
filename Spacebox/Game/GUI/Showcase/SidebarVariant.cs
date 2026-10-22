using Engine.CanvasUI;
using OpenTK.Mathematics;
using static Spacebox.Game.GUI.Showcase.ShowcaseParts;

namespace Spacebox.Game.GUI.Showcase;

public sealed class SidebarVariant : IInterfaceVariant
{
    private static readonly Color4 Panel = Rgba(10, 22, 20, 215);
    private static readonly Color4 Border = Rgba(50, 110, 95);
    private static readonly Color4 Accent = Rgba(90, 230, 160);
    private static readonly Color4 Bright = Rgba(225, 245, 235);
    private static readonly Color4 Dim = Rgba(130, 170, 155);
    private static readonly Color4 SlotBack = Rgba(20, 40, 36, 230);

    public string Name => "Sidebar";
    public string Description => "Vertical hotbar docked to the left edge, compact stats in the top right corner, one backpack window with item details on right click.";

    public void Build(ShowcaseView view)
    {
        var state = view.State;
        var bindings = view.Bindings;
        var popup = view.Popups.AddChild(new ItemDetailsPopup(Accent, Bright, Dim));

        var rail = view.Hud.AddChild(new PanelContainer { Name = "Rail", ThemeTypeVariation = "Panel" });
        rail.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterLeft, 12f);
        var slots = rail.AddChild(new VBoxContainer());
        slots.AddThemeConstantOverride("separation", 3f);
        for (int i = 0; i < state.Hotbar.Length; i++)
        {
            int index = i;
            var slot = slots.AddChild(new SlotControl(state.Hotbar[i], 40f) { ThemeTypeVariation = "HotbarSlot", Caption = ((i + 1) % 10).ToString() });
            slot.Clicked += _ => state.SelectedHotbar = index;
            slot.SecondaryClicked += popup.Show;
            bindings.Hotbar(slot);
        }

        var stats = view.Hud.AddChild(new PanelContainer { Name = "Stats", ThemeTypeVariation = "Panel", CustomMinimumSize = new Vector2(236f, 0f) });
        stats.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight, 12f);
        var statColumn = stats.AddChild(new VBoxContainer());
        statColumn.AddThemeConstantOverride("separation", 4f);
        AddStat(statColumn, bindings, "Health", Rgba(240, 100, 100), s => s.Health);
        AddStat(statColumn, bindings, "Energy", Rgba(240, 210, 90), s => s.Power);
        AddStat(statColumn, bindings, "Oxygen", Accent, s => s.Oxygen);

        var window = view.Windows.AddChild(new PanelContainer { Name = "Backpack", ThemeTypeVariation = "Window" });
        window.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        bindings.Window(window);

        var column = window.AddChild(new VBoxContainer());
        column.AddThemeConstantOverride("separation", 10f);
        var header = column.AddChild(new HBoxContainer());
        header.AddChild(Text("BACKPACK", Accent, 24f)).SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(Text($"{state.Inventory.Length} slots", Dim, 12f, HorizontalAlignment.Right));

        var body = column.AddChild(new HBoxContainer());
        body.AddThemeConstantOverride("separation", 14f);
        var grid = body.AddChild(InventoryGrid(state, 36f, 3f));
        for (int i = 0; i < grid.Children.Count; i++)
            if (grid.Children[i] is SlotControl slot) slot.SecondaryClicked += popup.Show;
        body.AddChild(new ColorRect(Border) { CustomMinimumSize = new Vector2(1f, 0f) });

        var suit = body.AddChild(new VBoxContainer());
        suit.AddThemeConstantOverride("separation", 6f);
        suit.AddChild(Text("SUIT", Accent, 12f));
        var suitGrid = suit.AddChild(new GridContainer { Columns = 3 });
        suitGrid.AddThemeConstantOverride("h_separation", 3f);
        suitGrid.AddThemeConstantOverride("v_separation", 3f);
        foreach (var slot in state.ArmorSlots) suitGrid.AddChild(EquipmentSlot(view.Assets, slot));
        foreach (var slot in state.DeviceSlots) suitGrid.AddChild(EquipmentSlot(view.Assets, slot));

        var footer = column.AddChild(new HBoxContainer());
        footer.AddThemeConstantOverride("separation", 6f);
        var hint = footer.AddChild(Text("Right click a slot for details, wheel selects the hotbar", Dim, 12f));
        hint.Overrun = TextOverrun.Ellipsis;
        hint.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        footer.AddChild(new Button("Sort") { TooltipText = "Sort by name" });
        footer.AddChild(new Button("Close") { TooltipText = "Hide the backpack" }).Pressed += _ =>
        {
            popup.Hide();
            bindings.SetWindowsVisible(false);
        };
    }

    private static SlotControl EquipmentSlot(ShowcaseAssets assets, (string Slot, string Icon) slot)
    {
        assets.EquipmentIcons.TryGetValue(slot.Icon, out var texture);
        return new SlotControl(null, 36f, texture) { TooltipText = slot.Slot };
    }

    private static void AddStat(VBoxContainer column, ShowcaseBindings bindings, string name, Color4 color, Func<ShowcaseState, float> value)
    {
        var row = column.AddChild(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 8f);
        row.AddChild(Text(name, Dim, 12f)).CustomMinimumSize = new Vector2(52f, 0f);
        var bar = row.AddChild(Bar(0f, 6f, color, Rgba(255, 255, 255, 24)));
        bar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        bar.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        var number = row.AddChild(Text("", Bright, 12f, HorizontalAlignment.Right));
        number.CustomMinimumSize = new Vector2(52f, 0f);
        bindings.Bar(bar, value, number);
    }

    public Engine.CanvasUI.Theme CreateTheme(ShowcaseAssets assets)
    {
        var theme = new Engine.CanvasUI.Theme { DefaultFont = assets.Font, DefaultFontSize = 12f };
        theme.SetStyleBox("Panel", "panel", new StyleBoxFlat(Panel, 8f).SetBorder(1f, Border));
        theme.SetStyleBox("Window", "panel", new StyleBoxFlat(Panel, 16f).SetBorder(1f, Border));
        theme.SetStyleBox("Popup", "panel", new StyleBoxFlat(Rgba(8, 18, 16, 245), 12f).SetBorder(1f, Accent));
        theme.SetStyleBox("Slot", "normal", new StyleBoxFlat(SlotBack).SetBorder(1f, Border));
        theme.SetStyleBox("Slot", "hover", new StyleBoxFlat(Rgba(30, 64, 56, 240)).SetBorder(1f, Accent));
        theme.SetStyleBox("Slot", "selected", new StyleBoxFlat(Rgba(30, 80, 60, 240)).SetBorder(2f, Accent));
        theme.SetTypeVariation("HotbarSlot", "Slot");
        theme.SetColor("Slot", "caption_color", Dim);
        theme.SetColor("Slot", "count_color", Bright);
        theme.SetColor("Slot", "placeholder_color", new Color4(0.6f, 1f, 0.8f, 0.25f));
        theme.SetStyleBox("Button", "normal", ButtonBox(Rgba(20, 44, 38), Border));
        theme.SetStyleBox("Button", "hover", ButtonBox(Rgba(30, 64, 56), Accent));
        theme.SetStyleBox("Button", "pressed", ButtonBox(Rgba(16, 32, 28), Accent));
        theme.SetColor("Button", "font_color", Bright);
        theme.SetStyleBox("TooltipPanel", "panel", new StyleBoxFlat(Rgba(8, 18, 16, 240), 6f).SetBorder(1f, Accent));
        theme.SetColor("TooltipLabel", "font_color", Bright);
        return theme;
    }

    private static StyleBoxFlat ButtonBox(Color4 background, Color4 border)
    {
        var box = new StyleBoxFlat(background).SetBorder(1f, border);
        box.SetContentMargins(10f, 10f);
        return box;
    }
}
