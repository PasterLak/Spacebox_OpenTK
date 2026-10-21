using Engine.CanvasUI;
using OpenTK.Mathematics;
using static Spacebox.Game.GUI.Showcase.ShowcaseParts;

namespace Spacebox.Game.GUI.Showcase;

public sealed class TutorialVariant : IInterfaceVariant
{
    private static readonly Color4 Panel = Rgba(16, 20, 28, 215);
    private static readonly Color4 Border = Rgba(90, 100, 120);
    private static readonly Color4 Gold = Rgba(255, 196, 40);
    private static readonly Color4 Green = Rgba(110, 220, 110);
    private static readonly Color4 Cyan = Rgba(110, 210, 230);
    private static readonly Color4 Bright = Rgba(240, 240, 240);
    private static readonly Color4 Dim = Rgba(185, 190, 200);
    private static readonly Color4 Faint = Rgba(130, 135, 145);
    private static readonly Color4 BarBack = Rgba(45, 50, 62);
    private static readonly Color4 Box = Rgba(170, 175, 185);

    public string Name => "Tutorial style";
    public string Description => "Dark translucent panels, thin borders and gold titles from the tutorial window; labelled slim bars.";

    public Control Build(ShowcaseAssets assets, ShowcaseState state, ShowcaseBindings bindings)
    {
        var root = new Control { Theme = CreateTheme(assets), MouseFilter = MouseFilter.Ignore };
        root.SetAnchorsPreset(LayoutPreset.FullRect);

        var hud = root.AddChild(new PanelContainer { ThemeTypeVariation = "HudPanel" });
        hud.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom, 10f, new Vector2(560f, 92f));
        var hudColumn = hud.AddChild(new VBoxContainer());
        hudColumn.AddThemeConstantOverride("separation", 6f);

        var bars = hudColumn.AddChild(new HBoxContainer());
        bars.AddThemeConstantOverride("separation", 14f);
        AddBar(bars, bindings, "HEALTH", Green, s => s.Health);
        AddBar(bars, bindings, "POWER", Cyan, s => s.Power);
        AddBar(bars, bindings, "OXYGEN", Gold, s => s.Oxygen);

        var hotbar = hudColumn.AddChild(Hotbar(state, bindings, 48f, 4f, true));
        hotbar.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;

        var windows = root.AddChild(new HBoxContainer { Alignment = BoxAlignment.Center });
        windows.AddThemeConstantOverride("separation", 12f);
        windows.SetAnchorsAndOffsetsPreset(LayoutPreset.Center, 0f, new Vector2(900f, 330f));
        windows.OffsetTop -= 60f;
        windows.OffsetBottom -= 60f;
        bindings.Window(windows);

        windows.AddChild(Window("INVENTORY", "Drag items to move them, right click to split", InventoryGrid(state, 42f, 4f)));

        var equipment = new HBoxContainer();
        equipment.AddThemeConstantOverride("separation", 10f);
        equipment.AddChild(SlotColumn(assets, state.ArmorSlots, 42f, 4f));
        var figure = equipment.AddChild(new PanelContainer { ThemeTypeVariation = "Figure", CustomMinimumSize = new Vector2(100f, 0f) });
        figure.AddChild(Text("player\npreview", Faint, 12f, HorizontalAlignment.Center));
        equipment.AddChild(SlotColumn(assets, state.DeviceSlots, 42f, 4f));
        windows.AddChild(Window("EQUIPMENT", "Armor and devices", equipment));

        return root;
    }

    private static void AddBar(HBoxContainer row, ShowcaseBindings bindings, string name, Color4 color, Func<ShowcaseState, float> value)
    {
        var column = row.AddChild(new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        column.AddThemeConstantOverride("separation", 2f);
        var header = column.AddChild(new HBoxContainer());
        header.AddChild(Text(name, Dim, 12f)).SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var number = header.AddChild(Text("", Bright, 12f, HorizontalAlignment.Right));
        var bar = column.AddChild(Bar(0f, 6f, color, BarBack));
        bindings.Bar(bar, value, number);
    }

    private static PanelContainer Window(string title, string hint, Control content)
    {
        var panel = new PanelContainer { ThemeTypeVariation = "Window", SizeFlagsVertical = SizeFlags.ShrinkBegin };
        var column = panel.AddChild(new VBoxContainer());
        column.AddThemeConstantOverride("separation", 8f);
        var header = column.AddChild(new HBoxContainer());
        header.AddChild(Text(title, Gold, 24f)).SizeFlagsHorizontal = SizeFlags.ExpandFill;
        column.AddChild(Text(hint, Faint, 12f));
        column.AddChild(new ColorRect(Border) { CustomMinimumSize = new Vector2(0f, 1f) });
        column.AddChild(content);
        return panel;
    }

    private static Engine.CanvasUI.Theme CreateTheme(ShowcaseAssets assets)
    {
        var theme = new Engine.CanvasUI.Theme { DefaultFont = assets.Font, DefaultFontSize = 12f };
        theme.SetStyleBox("Window", "panel", new StyleBoxFlat(Panel, 14f).SetBorder(1f, Border));
        theme.SetStyleBox("HudPanel", "panel", new StyleBoxFlat(Panel, 10f).SetBorder(1f, Border));
        theme.SetStyleBox("Figure", "panel", new StyleBoxFlat(Rgba(10, 13, 19, 200), 4f).SetBorder(1f, Border));
        theme.SetStyleBox("Slot", "normal", new StyleBoxFlat(BarBack).SetBorder(1f, Border));
        theme.SetStyleBox("Slot", "hover", new StyleBoxFlat(Rgba(58, 64, 80)).SetBorder(1f, Box));
        theme.SetStyleBox("Slot", "selected", new StyleBoxFlat(Rgba(58, 52, 30)).SetBorder(2f, Gold));
        theme.SetTypeVariation("HotbarSlot", "Slot");
        theme.SetColor("Slot", "caption_color", Faint);
        theme.SetColor("Slot", "count_color", Bright);
        theme.SetColor("Slot", "placeholder_color", new Color4(1f, 1f, 1f, 0.18f));
        theme.SetStyleBox("TooltipPanel", "panel", new StyleBoxFlat(Rgba(10, 13, 19, 240), 6f).SetBorder(1f, Gold));
        theme.SetColor("TooltipLabel", "font_color", Bright);
        return theme;
    }
}
