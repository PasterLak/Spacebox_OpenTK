using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Canvas.Items;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class CraftingWindow : GameWindow
{
    private const int VisibleItemRows = 3;
    private const int ScrollSettleFrames = 3;
    private const int DefaultInventoryColumns = 8;

    private readonly GridContainer _categories = new GridContainer();
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
        _categories.AddThemeConstantOverride("h_separation", 4f);
        _categories.AddThemeConstantOverride("v_separation", 4f);
        Body.AddChild(_categories);
        Body.AddChild(_itemsView);

        _items.AddThemeConstantOverride("h_separation", 0f);
        _items.AddThemeConstantOverride("v_separation", 0f);
        _itemsScroll.AddChild(_items);
        _itemsView.AddChild(_itemsScroll);

        var footer = _itemsView.AddChild(new HBoxContainer());
        footer.AddThemeConstantOverride("separation", 8f);
        var back = footer.AddChild(new Button { Text = "Back", CustomMinimumSize = new Vector2(64f, 26f) });
        back.Pressed += _ =>
        {
            CraftingGUI.PlayClick();
            ShowCategories();
        };
        footer.AddChild(_categoryTitle);
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
        _categories.ClearChildren();
        _categories.Columns = Math.Max(1, CraftingGUI.CategoryColumns);
        foreach (var category in _builtFor)
        {
            if (category == null)
            {
                _categories.AddChild(Spacer(CraftCategoryButton.DefaultSize.X, CraftCategoryButton.DefaultSize.Y));
                continue;
            }

            var button = _categories.AddChild(new CraftCategoryButton(category));
            button.Clicked += ShowItems;
        }

        ShowCategories();
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
        float spacing = GetThemeConstant("slot_spacing", 0f);
        float gridWidth = columns * ItemSlotControl.DefaultSize + (columns - 1) * spacing;
        float size = MathF.Floor((gridWidth - GetThemeConstant("scrollbar_width", 8f)) / columns);
        int count = category.Items.Count;
        int rows = Math.Max(1, (count + columns - 1) / columns);
        _items.Columns = columns;
        _itemsScroll.CustomMinimumSize = new Vector2(gridWidth, Math.Min(rows, VisibleItemRows) * size);

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
                else _items.AddChild(Spacer(size, size));
            }
        }

        _categories.Visible = false;
        _itemsView.Visible = true;
        _scrollToBottomFrames = ScrollSettleFrames;
    }
}
