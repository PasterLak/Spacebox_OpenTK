using Engine.CanvasUI;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class GameWindowsView : SafeArea
{
    public const float HudReserve = 120f;

    private readonly List<GameWindow> _windows = new List<GameWindow>();

    public GameWindowsView()
    {
        Name = "GameWindows";
        var margin = AddChild(new MarginContainer { MouseFilter = MouseFilter.Ignore });
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_bottom", HudReserve);

        var center = margin.AddChild(new CenterContainer { MouseFilter = MouseFilter.Ignore });
        var row = center.AddChild(new HBoxContainer { MouseFilter = MouseFilter.Ignore });
        row.AddThemeConstantOverride("separation", 10f);
        Left = row.AddChild(Column());
        Middle = row.AddChild(Column());
        Right = row.AddChild(Column());

        Inventory = new InventoryWindow();
        Add(Left, new CreativeWindow());
        Add(Middle, new StorageWindow());
        Add(Middle, Inventory);
        Add(Right, new EquipmentWindow());
    }

    public VBoxContainer Left { get; }
    public VBoxContainer Middle { get; }
    public VBoxContainer Right { get; }
    public InventoryWindow Inventory { get; }

    public void Add(VBoxContainer column, GameWindow window)
    {
        _windows.Add(window);
        column.AddChild(window);
    }

    public void Sync()
    {
        for (int i = 0; i < _windows.Count; i++) _windows[i].Sync();
    }

    private static VBoxContainer Column()
    {
        var column = new VBoxContainer { MouseFilter = MouseFilter.Ignore, SizeFlagsVertical = SizeFlags.ShrinkEnd };
        column.AddThemeConstantOverride("separation", 10f);
        return column;
    }
}
