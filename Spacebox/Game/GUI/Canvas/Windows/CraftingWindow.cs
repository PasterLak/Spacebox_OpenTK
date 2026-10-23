using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Canvas.Items;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class CraftingWindow : GameWindow
{
    private const int VisibleItemRows = 3;
    private const int ScrollSettleFrames = 3;
    private const int DefaultInventoryColumns = 8;

    private const float TileGap = 4f;

    private readonly VBoxContainer _categories = new VBoxContainer();
    private readonly HBoxContainer _tabs = new HBoxContainer();
    private readonly GridContainer _tiles = new GridContainer();
    private readonly List<Button> _tabButtons = new List<Button>();
    private readonly VBoxContainer _itemsView = new VBoxContainer { Visible = false };
    private readonly Label _categoryTitle = new Label { ThemeTypeVariation = "SectionLabel", SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
    private readonly GridContainer _items = new GridContainer();
    private readonly ScrollContainer _itemsScroll = new ScrollContainer();
    private readonly List<CraftItemControl> _itemControls = new List<CraftItemControl>();
    private CraftingCategory[] _builtFor;
    private int _scrollToBottomFrames;

    public CraftingWindow() : base("Crafting")
    {
        Name = "Crafting";
        MatchColumnWidth();
        _tabs.AddThemeConstantOverride("separation", 4f);
        _tiles.AddThemeConstantOverride("h_separation", TileGap);
        _tiles.AddThemeConstantOverride("v_separation", TileGap);
        _categories.AddChild(_tabs);
        _categories.AddChild(_tiles);
        Body.AddChild(_categories);
        Body.AddChild(_itemsView);

        _items.AddThemeConstantOverride("h_separation", 0f);
        _items.AddThemeConstantOverride("v_separation", 0f);
        _itemsScroll.AddChild(_items);
        _itemsView.AddChild(_itemsScroll);

        var footer = _itemsView.AddChild(new HBoxContainer());
        footer.AddThemeConstantOverride("separation", 8f);
        footer.AddChild(_categoryTitle);
        var back = footer.AddChild(new Button { Text = "Back", CustomMinimumSize = new Vector2(64f, 26f) });
        back.Pressed += _ =>
        {
            CraftingGUI.PlayClick();
            ShowCategories();
        };
    }

    public override bool IsOpen => CraftingGUI.IsVisible && CraftingGUI.Categories != null;

    protected override void Refresh()
    {
        if (!ReferenceEquals(_builtFor, CraftingGUI.Categories)) BuildCategories();
        if (!_itemsView.Visible) return;
        if (_scrollToBottomFrames > 0)
        {
            _scrollToBottomFrames--;
            _itemsScroll.ScrollOffset = _itemsScroll.MaxScroll;
        }

        foreach (var control in _itemControls) control.Refresh();
    }

    private void BuildCategories()
    {
        _builtFor = CraftingGUI.Categories;
        _tabs.ClearChildren();
        _tabButtons.Clear();
        var groups = CraftingGUI.Groups;
        for (int i = 0; i < groups.Count; i++)
        {
            int index = i;
            var tab = _tabs.AddChild(new Button { Text = groups[i].Name, ToggleMode = true, SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0f, 24f) });
            tab.Pressed += _ =>
            {
                CraftingGUI.PlayClick();
                ShowGroup(index);
            };
            tab.MouseEntered += _ =>
            {
                if (CraftingGUI.SelectedGroup == index) return;
                CraftingGUI.PlayHover(index);
                ShowGroup(index);
            };
            _tabButtons.Add(tab);
        }
        _tabs.Visible = groups.Count > 1;
        ShowGroup(Math.Clamp(CraftingGUI.SelectedGroup, 0, Math.Max(0, groups.Count - 1)));
        ShowCategories();
    }

    private void ShowGroup(int index)
    {
        var groups = CraftingGUI.Groups;
        CraftingGUI.SelectedGroup = index;
        for (int i = 0; i < _tabButtons.Count; i++) _tabButtons[i].ButtonPressed = i == index;

        var tile = CraftCategoryButton.DefaultSize;
        float width = GridWidth;
        int columns = Math.Max(1, (int)((width + TileGap) / (tile.X + TileGap)));
        var size = new Vector2(MathF.Floor((width - (columns - 1) * TileGap) / columns), tile.Y);
        int rows = 1;
        foreach (var group in groups) rows = Math.Max(rows, (group.Categories.Count + columns - 1) / columns);

        _tiles.ClearChildren();
        _tiles.Columns = columns;
        var categories = index < groups.Count ? groups[index].Categories : new List<CraftingCategory>();
        for (int row = rows - 1; row >= 0; row--)
        {
            for (int column = 0; column < columns; column++)
            {
                int i = row * columns + column;
                if (i < categories.Count)
                {
                    var button = _tiles.AddChild(new CraftCategoryButton(categories[i]) { CustomMinimumSize = size });
                    button.Clicked += ShowItems;
                }
                else _tiles.AddChild(Spacer(size.X, size.Y));
            }
        }
    }

    private float GridWidth
    {
        get
        {
            int columns = InventoryColumns;
            return columns * ItemSlotControl.DefaultSize + (columns - 1) * GetThemeConstant("slot_spacing", 0f);
        }
    }

    private static int InventoryColumns => Math.Max(1, (int)(InventoryUI.Player?.Inventory?.SizeX ?? DefaultInventoryColumns));

    private void ShowCategories()
    {
        _categories.Visible = true;
        _itemsView.Visible = false;
        TitleLabel.Text = "Crafting";
    }

    private void ShowItems(CraftingCategory category)
    {
        CraftingGUI.PlayClick();
        _categoryTitle.Text = category.Name;
        _items.ClearChildren();
        _itemControls.Clear();
        int columns = InventoryColumns;
        float gridWidth = GridWidth;
        float size = MathF.Floor((gridWidth - GetThemeConstant("scrollbar_width", 8f)) / columns);
        int count = category.Items.Count;
        int rows = Math.Max(VisibleItemRows, (count + columns - 1) / columns);
        _items.Columns = columns;
        _itemsScroll.CustomMinimumSize = new Vector2(gridWidth, VisibleItemRows * size);

        var controls = new CraftItemControl[count];
        for (int i = 0; i < count; i++)
        {
            int id = i;
            var control = new CraftItemControl(category.Items[i], size);
            controls[i] = control;
            control.Hovered += _ => CraftingGUI.PlayHover(id);
            control.Clicked += c =>
            {
                if (c.Data?.blueprint != null) CraftingGUI.Craft(c.Data.blueprint);
            };
            control.Refresh();
            _itemControls.Add(control);
        }

        for (int row = rows - 1; row >= 0; row--)
        {
            for (int column = 0; column < columns; column++)
            {
                int index = row * columns + column;
                if (index < count) _items.AddChild(controls[index]);
                else _items.AddChild(new EmptySlotControl(size));
            }
        }

        _categories.Visible = false;
        _itemsView.Visible = true;
        _scrollToBottomFrames = ScrollSettleFrames;
    }
}
