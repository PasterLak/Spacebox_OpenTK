using Engine.CanvasUI;
using OpenTK.Mathematics;
using static Spacebox.Game.GUI.Showcase.ShowcaseParts;

namespace Spacebox.Game.GUI.Showcase;

public sealed class TerminalVariant : IInterfaceVariant
{
    private static readonly Color4 Screen = Rgba(14, 9, 2, 225);
    private static readonly Color4 Amber = Rgba(255, 176, 0);
    private static readonly Color4 AmberDim = Rgba(170, 115, 20);
    private static readonly Color4 AmberFaint = Rgba(110, 72, 12);
    private static readonly Color4 Cell = Rgba(30, 20, 4, 230);

    private static readonly string[] Messages =
    {
        "> Oxygen reserve below 60 percent. Return to the base or find an oxygen generator.",
        "> Drill tier 2 required for the iron vein at sector 7G.",
        "> Docking computer offline, manual approach only."
    };

    public string Name => "Terminal";
    public string Description => "Amber monochrome terminal: comms log in the corner, text status block, bracketed hotbar and two manifest windows.";

    public void Build(ShowcaseView view)
    {
        var state = view.State;
        var bindings = view.Bindings;

        var log = view.Hud.AddChild(new PanelContainer { Name = "Comms", ThemeTypeVariation = "Screen", CustomMinimumSize = new Vector2(330f, 0f), CustomMaximumSize = new Vector2(330f, 0f) });
        log.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft, 12f);
        var logColumn = log.AddChild(new VBoxContainer());
        logColumn.AddThemeConstantOverride("separation", 3f);
        logColumn.AddChild(Text("COMMS // CHANNEL 3", Amber, 12f));
        foreach (var message in Messages)
        {
            var line = logColumn.AddChild(Text(message, AmberDim, 12f));
            line.Overrun = TextOverrun.Ellipsis;
            line.TooltipText = message.Substring(2);
            line.MouseFilter = MouseFilter.Pass;
        }

        var status = view.Hud.AddChild(new PanelContainer { Name = "Status", ThemeTypeVariation = "Screen" });
        status.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft, 12f);
        var statusGrid = status.AddChild(new GridContainer { Columns = 3 });
        statusGrid.AddThemeConstantOverride("h_separation", 10f);
        statusGrid.AddThemeConstantOverride("v_separation", 4f);
        AddStatus(statusGrid, bindings, "HULL", s => s.Health);
        AddStatus(statusGrid, bindings, "CELL", s => s.Power);
        AddStatus(statusGrid, bindings, "O2", s => s.Oxygen);

        var hotbarPanel = view.Hud.AddChild(new PanelContainer { Name = "Hotbar", ThemeTypeVariation = "Screen" });
        hotbarPanel.AddChild(Hotbar(state, bindings, 40f, 2f, true));
        hotbarPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom, 12f);

        var windows = view.Windows.AddChild(new HBoxContainer { Name = "Windows", Alignment = BoxAlignment.Center });
        windows.AddThemeConstantOverride("separation", 10f);
        windows.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        bindings.Window(windows);

        windows.AddChild(Window("CARGO HOLD // MANIFEST 7G // PRIORITY", InventoryGrid(state, 38f, 2f), "F: filter  S: sort"));

        var suit = new VBoxContainer();
        suit.AddThemeConstantOverride("separation", 6f);
        var suitGrid = suit.AddChild(new GridContainer { Columns = 3 });
        suitGrid.AddThemeConstantOverride("h_separation", 2f);
        suitGrid.AddThemeConstantOverride("v_separation", 2f);
        foreach (var slot in state.ArmorSlots) suitGrid.AddChild(EquipmentSlot(view.Assets, slot));
        foreach (var slot in state.DeviceSlots) suitGrid.AddChild(EquipmentSlot(view.Assets, slot));
        suit.AddChild(Text("INTEGRITY 92%", AmberDim, 12f));
        windows.AddChild(Window("SUIT", suit, "[EQUIP]"));
    }

    private static PanelContainer Window(string title, Control content, string footer)
    {
        var panel = new PanelContainer { ThemeTypeVariation = "Window", SizeFlagsVertical = SizeFlags.ShrinkBegin };
        var column = panel.AddChild(new VBoxContainer());
        column.AddThemeConstantOverride("separation", 6f);
        var header = column.AddChild(Text(title, Amber, 12f));
        header.Overrun = TextOverrun.Ellipsis;
        column.AddChild(new ColorRect(AmberFaint) { CustomMinimumSize = new Vector2(0f, 1f) });
        column.AddChild(content);
        column.AddChild(Text(footer, AmberFaint, 12f, HorizontalAlignment.Right));
        return panel;
    }

    private static SlotControl EquipmentSlot(ShowcaseAssets assets, (string Slot, string Icon) slot)
    {
        assets.EquipmentIcons.TryGetValue(slot.Icon, out var texture);
        return new SlotControl(null, 38f, texture) { TooltipText = slot.Slot };
    }

    private static void AddStatus(GridContainer grid, ShowcaseBindings bindings, string name, Func<ShowcaseState, float> value)
    {
        grid.AddChild(Text(name, Amber, 12f)).CustomMinimumSize = new Vector2(36f, 0f);
        var bar = grid.AddChild(Bar(140f, 8f, Amber, AmberFaint));
        bar.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        var number = grid.AddChild(Text("", AmberDim, 12f, HorizontalAlignment.Right));
        number.CustomMinimumSize = new Vector2(56f, 0f);
        bindings.Bar(bar, value, number);
    }

    public Engine.CanvasUI.Theme CreateTheme(ShowcaseAssets assets)
    {
        var theme = new Engine.CanvasUI.Theme { DefaultFont = assets.Font, DefaultFontSize = 12f };
        theme.SetStyleBox("Screen", "panel", new StyleBoxFlat(Screen, 8f).SetBorder(1f, AmberFaint));
        var window = new StyleBoxFlat(Screen, 12f).SetBorder(1f, AmberDim);
        theme.SetStyleBox("Window", "panel", window);
        theme.SetStyleBox("Slot", "normal", new StyleBoxFlat(Cell).SetBorder(1f, AmberFaint));
        theme.SetStyleBox("Slot", "hover", new StyleBoxFlat(Rgba(60, 40, 6, 240)).SetBorder(1f, AmberDim));
        theme.SetStyleBox("Slot", "selected", new StyleBoxFlat(Rgba(80, 52, 4, 240)).SetBorder(2f, Amber));
        theme.SetTypeVariation("HotbarSlot", "Slot");
        theme.SetColor("Slot", "caption_color", AmberDim);
        theme.SetColor("Slot", "count_color", Amber);
        theme.SetColor("Slot", "placeholder_color", new Color4(1f, 0.7f, 0.1f, 0.25f));
        theme.SetStyleBox("TooltipPanel", "panel", new StyleBoxFlat(Rgba(20, 13, 2, 245), 6f).SetBorder(1f, Amber));
        theme.SetColor("TooltipLabel", "font_color", Amber);
        return theme;
    }
}
