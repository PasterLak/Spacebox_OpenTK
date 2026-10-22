using Engine.CanvasUI;
using OpenTK.Mathematics;
using static Spacebox.Game.GUI.Showcase.ShowcaseParts;

namespace Spacebox.Game.GUI.Showcase;

public sealed class ShowcaseHelp
{
    private static readonly (string Keys, string Action)[] Lines =
    {
        ("1-5 / F1-F5", "switch the interface"),
        ("Tab / I", "show or hide the windows"),
        ("Mouse wheel", "select a hotbar slot"),
        ("Right click", "item details (Sidebar)"),
        ("+ / -", "interface scale"),
        ("F8", "layout check overlay"),
        ("Esc", "close this window / back to the menu")
    };

    private readonly ColorRect _backdrop;

    public bool IsOpen => _backdrop.Visible;
    public PanelContainer Dialog { get; }
    public Button CloseButton { get; }

    public ShowcaseHelp(CanvasRoot root, Engine.CanvasUI.Theme theme)
    {
        _backdrop = root.Add(new ColorRect(Rgba(0, 0, 0, 150)) { Name = "HelpBackdrop", MouseFilter = MouseFilter.Stop, Theme = theme, Visible = false }, UiLayer.Modal);
        _backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        var area = _backdrop.AddChild(new SafeArea());

        Dialog = area.AddChild(new PanelContainer { Name = "HelpDialog", ThemeTypeVariation = "Dialog", CustomMaximumSize = new Vector2(560f, 0f) });
        Dialog.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);

        var column = Dialog.AddChild(new VBoxContainer());
        column.AddThemeConstantOverride("separation", 8f);
        column.AddChild(Text("CONTROLS", Rgba(255, 196, 40), 24f));

        var grid = column.AddChild(new GridContainer { Columns = 2 });
        grid.AddThemeConstantOverride("h_separation", 18f);
        grid.AddThemeConstantOverride("v_separation", 4f);
        foreach (var (keys, action) in Lines)
        {
            grid.AddChild(Text(keys, Rgba(240, 240, 240), 12f));
            grid.AddChild(Text(action, Rgba(185, 190, 200), 12f));
        }

        CloseButton = column.AddChild(new Button("Close") { SizeFlagsHorizontal = SizeFlags.ShrinkEnd, CustomMinimumSize = new Vector2(96f, 0f) });
        CloseButton.Pressed += _ => Close();
    }

    public void Open() => _backdrop.Visible = true;

    public void Close() => _backdrop.Visible = false;
}
