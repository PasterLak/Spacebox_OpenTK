using Engine.CanvasUI;
using OpenTK.Mathematics;
using static Spacebox.Game.GUI.Showcase.ShowcaseParts;

namespace Spacebox.Game.GUI.Showcase;

public sealed class ClassicVariant : IInterfaceVariant
{
    public string Name => "Classic";
    public string Description => "The current look rebuilt on the new UI: bevelled grey slots, coloured bars above the hotbar, grey windows.";

    public void Build(ShowcaseView view)
    {
        var state = view.State;
        var bindings = view.Bindings;
        var assets = view.Assets;

        var hud = view.Hud.AddChild(new VBoxContainer { Name = "Hud" });
        hud.AddThemeConstantOverride("separation", 4f);
        hud.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom, 12f);

        var bars = hud.AddChild(new HBoxContainer { Alignment = BoxAlignment.Center });
        bars.AddThemeConstantOverride("separation", 48f);
        AddBar(bars, bindings, Rgba(200, 0, 0), s => s.Health);
        AddBar(bars, bindings, Rgba(0, 200, 210), s => s.Power);
        AddBar(bars, bindings, Rgba(0, 0, 210), s => s.Oxygen);

        var hotbar = hud.AddChild(Hotbar(state, bindings, 44f, 2f, false));
        hotbar.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;

        var windows = view.Windows.AddChild(new HBoxContainer { Name = "Windows", Alignment = BoxAlignment.Center });
        windows.AddThemeConstantOverride("separation", 16f);
        windows.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        bindings.Window(windows);

        windows.AddChild(Window("Inventory", InventoryGrid(state, 40f, 2f)));

        var equipment = new HBoxContainer();
        equipment.AddThemeConstantOverride("separation", 8f);
        equipment.AddChild(SlotColumn(assets, state.ArmorSlots, 40f, 2f));
        var figure = equipment.AddChild(new Panel { ThemeTypeVariation = "Figure", CustomMinimumSize = new Vector2(90f, 0f) });
        figure.AddChild(Text("3D", Rgba(90, 90, 90), 24f, HorizontalAlignment.Center)).SetAnchorsPreset(LayoutPreset.FullRect);
        equipment.AddChild(SlotColumn(assets, state.DeviceSlots, 40f, 2f));
        windows.AddChild(Window("Equipment", equipment));
    }

    private static void AddBar(HBoxContainer row, ShowcaseBindings bindings, Color4 fill, Func<ShowcaseState, float> value)
    {
        var bar = row.AddChild(Bar(110f, 16f, fill, Rgba(40, 40, 40)));
        var text = bar.AddChild(Text("", Color4.White, 0f, HorizontalAlignment.Center));
        text.SetAnchorsPreset(LayoutPreset.FullRect);
        bindings.Bar(bar, value, text);
    }

    private static PanelContainer Window(string title, Control content)
    {
        var panel = new PanelContainer { ThemeTypeVariation = "Window", SizeFlagsVertical = SizeFlags.ShrinkBegin };
        var column = panel.AddChild(new VBoxContainer());
        column.AddThemeConstantOverride("separation", 6f);
        column.AddChild(Text(title, Rgba(30, 30, 30), 24f));
        column.AddChild(content);
        return panel;
    }

    public Engine.CanvasUI.Theme CreateTheme(ShowcaseAssets assets)
    {
        var bevel = new StyleBoxFlat(Rgba(190, 190, 190), 10f) { BorderColor = Rgba(90, 90, 90), HighlightColor = Rgba(235, 235, 235), HighlightSize = 2f };
        bevel.SetBorder(2f, Rgba(90, 90, 90));
        var theme = new Engine.CanvasUI.Theme { DefaultFont = assets.Font, DefaultFontSize = 12f };
        theme.SetStyleBox("Window", "panel", bevel);
        theme.SetStyleBox("Figure", "panel", new StyleBoxFlat(Rgba(150, 150, 150)).SetBorder(2f, Rgba(110, 110, 110)));
        theme.SetStyleBox("Slot", "normal", new StyleBoxTexture(assets.Slot, 3f, 0f));
        theme.SetStyleBox("Slot", "hover", new StyleBoxTexture(assets.Slot, 3f, 0f) { Modulate = Rgba(225, 225, 255) });
        theme.SetStyleBox("Slot", "selected", new StyleBoxTexture(assets.SelectedSlot, 3f, 0f));
        theme.SetTypeVariation("HotbarSlot", "Slot");
        theme.SetColor("Slot", "count_color", Color4.White);
        theme.SetColor("Slot", "placeholder_color", new Color4(0f, 0f, 0f, 0.35f));
        theme.SetStyleBox("TooltipPanel", "panel", new StyleBoxFlat(Rgba(20, 20, 20, 230), 6f).SetBorder(1f, Rgba(120, 120, 120)));
        return theme;
    }
}
