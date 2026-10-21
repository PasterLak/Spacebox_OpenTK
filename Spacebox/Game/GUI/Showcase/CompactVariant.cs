using Engine.CanvasUI;
using OpenTK.Mathematics;
using static Spacebox.Game.GUI.Showcase.ShowcaseParts;

namespace Spacebox.Game.GUI.Showcase;

public sealed class CompactVariant : IInterfaceVariant
{
    private static readonly Color4 Glass = Rgba(8, 18, 30, 190);
    private static readonly Color4 Accent = Rgba(80, 210, 255);
    private static readonly Color4 Line = Rgba(60, 110, 140, 200);
    private static readonly Color4 Text1 = Rgba(225, 240, 250);
    private static readonly Color4 Text2 = Rgba(120, 160, 185);

    public string Name => "Compact";
    public string Description => "Minimal HUD: stacked bars in the corner, slim hotbar with key numbers, inventory and equipment in a docked side drawer.";

    public Control Build(ShowcaseAssets assets, ShowcaseState state, ShowcaseBindings bindings)
    {
        var root = new Control { Theme = CreateTheme(assets), MouseFilter = MouseFilter.Ignore };
        root.SetAnchorsPreset(LayoutPreset.FullRect);

        var stats = root.AddChild(new PanelContainer { ThemeTypeVariation = "Glass" });
        stats.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft, 14f, new Vector2(210f, 78f));
        var statColumn = stats.AddChild(new VBoxContainer());
        statColumn.AddThemeConstantOverride("separation", 5f);
        AddStat(statColumn, bindings, "HP", Rgba(255, 90, 90), s => s.Health);
        AddStat(statColumn, bindings, "EN", Accent, s => s.Power);
        AddStat(statColumn, bindings, "O2", Rgba(150, 255, 200), s => s.Oxygen);

        var hotbarPanel = root.AddChild(new PanelContainer { ThemeTypeVariation = "Glass" });
        hotbarPanel.AddChild(Hotbar(state, bindings, 38f, 3f, true));
        hotbarPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom, 14f);

        var drawer = root.AddChild(new PanelContainer { ThemeTypeVariation = "Drawer" });
        drawer.SetAnchorsPreset(LayoutPreset.RightWide);
        drawer.SetOffsets(-470f, 72f, -8f, -84f);
        drawer.GrowHorizontal = GrowDirection.Begin;
        bindings.Window(drawer);

        var column = drawer.AddChild(new VBoxContainer());
        column.AddThemeConstantOverride("separation", 10f);
        column.AddChild(Text("CARGO", Accent, 24f));
        column.AddChild(Text("40 slots  ·  mass 1.2 t", Text2, 12f));
        column.AddChild(InventoryGrid(state, 40f, 3f));
        column.AddChild(new ColorRect(Line) { CustomMinimumSize = new Vector2(0f, 1f) });
        column.AddChild(Text("SUIT", Accent, 24f));

        var suit = column.AddChild(new HBoxContainer());
        suit.AddThemeConstantOverride("separation", 3f);
        foreach (var slot in state.ArmorSlots) suit.AddChild(EquipmentSlot(assets, slot));
        suit.AddChild(Spacer(12f));
        foreach (var slot in state.DeviceSlots) suit.AddChild(EquipmentSlot(assets, slot));

        column.AddChild(Spacer(0f, 0f, true));
        column.AddChild(Text("Tab: close drawer", Text2, 12f));
        return root;
    }

    private static SlotControl EquipmentSlot(ShowcaseAssets assets, (string Slot, string Icon) slot)
    {
        assets.EquipmentIcons.TryGetValue(slot.Icon, out var texture);
        return new SlotControl(null, 40f, texture) { TooltipText = slot.Slot };
    }

    private static void AddStat(VBoxContainer column, ShowcaseBindings bindings, string name, Color4 color, Func<ShowcaseState, float> value)
    {
        var row = column.AddChild(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 8f);
        row.AddChild(Text(name, color, 12f)).CustomMinimumSize = new Vector2(20f, 0f);
        var bar = row.AddChild(Bar(0f, 8f, color, Rgba(255, 255, 255, 30)));
        bar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        bar.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        var number = row.AddChild(Text("", Text1, 12f, HorizontalAlignment.Right));
        number.CustomMinimumSize = new Vector2(52f, 0f);
        bindings.Bar(bar, value, number);
    }

    private static Engine.CanvasUI.Theme CreateTheme(ShowcaseAssets assets)
    {
        var theme = new Engine.CanvasUI.Theme { DefaultFont = assets.Font, DefaultFontSize = 12f };
        theme.SetStyleBox("Glass", "panel", new StyleBoxFlat(Glass, 7f).SetBorder(1f, Line));
        var drawer = new StyleBoxFlat(Rgba(6, 14, 24, 225), 18f);
        drawer.BorderLeft = 2f;
        drawer.BorderColor = Accent;
        theme.SetStyleBox("Drawer", "panel", drawer);
        theme.SetStyleBox("Slot", "normal", new StyleBoxFlat(Rgba(255, 255, 255, 14)).SetBorder(1f, Line));
        theme.SetStyleBox("Slot", "hover", new StyleBoxFlat(Rgba(80, 210, 255, 40)).SetBorder(1f, Accent));
        theme.SetStyleBox("Slot", "selected", new StyleBoxFlat(Rgba(80, 210, 255, 60)).SetBorder(2f, Accent));
        theme.SetTypeVariation("HotbarSlot", "Slot");
        theme.SetColor("Slot", "caption_color", Text2);
        theme.SetColor("Slot", "count_color", Text1);
        theme.SetColor("Slot", "placeholder_color", new Color4(0.5f, 0.85f, 1f, 0.3f));
        theme.SetStyleBox("TooltipPanel", "panel", new StyleBoxFlat(Rgba(6, 14, 24, 240), 6f).SetBorder(1f, Accent));
        theme.SetColor("TooltipLabel", "font_color", Text1);
        return theme;
    }
}
