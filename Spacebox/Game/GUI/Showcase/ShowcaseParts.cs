using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Showcase;

public interface IInterfaceVariant
{
    string Name { get; }
    string Description { get; }
    Engine.CanvasUI.Theme CreateTheme(ShowcaseAssets assets);
    void Build(ShowcaseView view);
}

public sealed class ShowcaseView
{
    public Control Hud { get; init; }
    public Control Windows { get; init; }
    public Control Popups { get; init; }
    public ShowcaseAssets Assets { get; init; }
    public ShowcaseState State { get; init; }
    public ShowcaseBindings Bindings { get; init; }
}

public sealed class ShowcaseBindings
{
    private readonly List<(ProgressBar Bar, Func<ShowcaseState, float> Value, Label Text)> _bars = new();
    private readonly List<SlotControl> _hotbar = new();
    private readonly List<Control> _windows = new();
    private readonly Dictionary<Label, int> _lastValues = new();

    public bool WindowsVisible { get; private set; } = true;

    public void Bar(ProgressBar bar, Func<ShowcaseState, float> value, Label text = null) => _bars.Add((bar, value, text));

    public void Hotbar(SlotControl slot) => _hotbar.Add(slot);

    public void Window(Control window) => _windows.Add(window);

    public IReadOnlyList<SlotControl> HotbarSlots => _hotbar;

    public void SetWindowsVisible(bool visible)
    {
        WindowsVisible = visible;
        foreach (var window in _windows) window.Visible = visible;
    }

    public void Update(ShowcaseState state)
    {
        foreach (var (bar, value, text) in _bars)
        {
            bar.Value = value(state);
            if (text == null) continue;
            int rounded = (int)MathF.Round(bar.Value);
            if (_lastValues.TryGetValue(text, out int last) && last == rounded) continue;
            _lastValues[text] = rounded;
            text.Text = $"{rounded}/{(int)bar.MaxValue}";
        }
        for (int i = 0; i < _hotbar.Count; i++) _hotbar[i].Selected = i == state.SelectedHotbar;
    }
}

public static class ShowcaseParts
{
    public static HBoxContainer Hotbar(ShowcaseState state, ShowcaseBindings bindings, float slotSize, float separation, bool numbered)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", separation);
        for (int i = 0; i < state.Hotbar.Length; i++)
        {
            int index = i;
            var slot = row.AddChild(new SlotControl(state.Hotbar[i], slotSize) { ThemeTypeVariation = "HotbarSlot" });
            if (numbered) slot.Caption = ((i + 1) % 10).ToString();
            slot.Clicked += _ => state.SelectedHotbar = index;
            bindings.Hotbar(slot);
        }
        return row;
    }

    public static GridContainer InventoryGrid(ShowcaseState state, float slotSize, float separation)
    {
        var grid = new GridContainer { Columns = ShowcaseState.InventoryColumns };
        grid.AddThemeConstantOverride("h_separation", separation);
        grid.AddThemeConstantOverride("v_separation", separation);
        foreach (var slot in state.Inventory) grid.AddChild(new SlotControl(slot, slotSize));
        return grid;
    }

    public static VBoxContainer SlotColumn(ShowcaseAssets assets, IReadOnlyList<(string Slot, string Icon)> slots, float slotSize, float separation, string variation = "Slot")
    {
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", separation);
        foreach (var (name, icon) in slots)
        {
            assets.EquipmentIcons.TryGetValue(icon, out var texture);
            column.AddChild(new SlotControl(null, slotSize, texture) { TooltipText = name, ThemeTypeVariation = variation });
        }
        return column;
    }

    public static ProgressBar Bar(float width, float height, Color4 fill, Color4 background)
    {
        var bar = new ProgressBar { CustomMinimumSize = new Vector2(width, height), ShowPercentage = false, MaxValue = 100f };
        bar.AddThemeStyleBoxOverride("fill", new StyleBoxFlat(fill));
        bar.AddThemeStyleBoxOverride("background", new StyleBoxFlat(background));
        return bar;
    }

    public static Label Text(string text, Color4 color, float size = 0f, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label(text) { FontColor = color, HorizontalAlignment = align, VerticalAlignment = VerticalAlignment.Center };
        if (size > 0f) label.AddThemeFontSizeOverride(size);
        return label;
    }

    public static Control Spacer(float width = 0f, float height = 0f, bool expand = false)
    {
        return new Control
        {
            CustomMinimumSize = new Vector2(width, height),
            MouseFilter = MouseFilter.Ignore,
            SizeFlagsHorizontal = expand ? SizeFlags.ExpandFill : SizeFlags.Fill,
            SizeFlagsVertical = expand ? SizeFlags.ExpandFill : SizeFlags.Fill
        };
    }

    public static Color4 Rgba(int r, int g, int b, int a = 255) => new Color4(r / 255f, g / 255f, b / 255f, a / 255f);
}
