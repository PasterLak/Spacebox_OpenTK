using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Canvas.Items;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class CraftingWindow : GameWindow
{
    private const int VisibleItemRows = 4;
    private const float DetailsHeight = 120f;
    private const float IngredientIconSize = 20f;

    private readonly GridContainer _categories = new GridContainer();
    private readonly VBoxContainer _itemsView = new VBoxContainer { Visible = false };
    private readonly Label _categoryTitle = new Label { ThemeTypeVariation = "SectionLabel", SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
    private readonly GridContainer _items = new GridContainer();
    private readonly ScrollContainer _itemsScroll = new ScrollContainer();
    private readonly List<CraftItemControl> _itemControls = new List<CraftItemControl>();
    private readonly Label _detailsName = new Label { Bold = true };
    private readonly Label _detailsDescription = new Label { ThemeTypeVariation = "MutedLabel", Autowrap = true };
    private readonly VBoxContainer _ingredients = new VBoxContainer();
    private readonly List<(Label Label, Ingredient Ingredient)> _ingredientLabels = new List<(Label, Ingredient)>();
    private CraftingCategory[] _builtFor;
    private CraftItemControl _detailsFor;

    public CraftingWindow() : base("Crafting")
    {
        Name = "Crafting";
        _categories.AddThemeConstantOverride("h_separation", 4f);
        _categories.AddThemeConstantOverride("v_separation", 4f);
        Body.AddChild(_categories);
        Body.AddChild(_itemsView);

        var header = _itemsView.AddChild(new HBoxContainer());
        var back = header.AddChild(new Button { Text = "Back", ThemeTypeVariation = "SmallButton" });
        back.Pressed += _ =>
        {
            CraftingGUI.PlayClick();
            ShowCategories();
        };
        header.AddChild(_categoryTitle);

        _items.AddThemeConstantOverride("h_separation", 0f);
        _items.AddThemeConstantOverride("v_separation", 0f);
        _itemsScroll.AddChild(_items);
        _itemsView.AddChild(_itemsScroll);

        var details = _itemsView.AddChild(new PanelContainer { ThemeTypeVariation = "InsetPanel", CustomMinimumSize = new Vector2(0f, DetailsHeight) });
        var column = details.AddChild(new VBoxContainer());
        column.AddThemeConstantOverride("separation", 3f);
        column.AddChild(_detailsName);
        column.AddChild(_detailsDescription);
        column.AddChild(_ingredients);
        _ingredients.AddThemeConstantOverride("separation", 2f);
    }

    public override bool IsOpen => CraftingGUI.IsVisible && CraftingGUI.Categories != null;

    protected override void Refresh()
    {
        if (!ReferenceEquals(_builtFor, CraftingGUI.Categories)) BuildCategories();
        if (!_itemsView.Visible) return;

        foreach (var control in _itemControls) control.Refresh();
        foreach (var (label, ingredient) in _ingredientLabels) StatusText.SetKind(label, KindOf(ingredient));
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

        float width = Math.Max(1, CraftingGUI.CategoryColumns) * (CraftCategoryButton.DefaultSize.X + 4f);
        _itemsScroll.CustomMinimumSize = new Vector2(width, VisibleItemRows * ItemSlotControl.DefaultSize);
        _detailsDescription.CustomMinimumSize = new Vector2(width - 20f, 0f);
        ShowCategories();
    }

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
        int columns = Math.Max(1, (int)((_itemsScroll.CustomMinimumSize.X - 12f) / ItemSlotControl.DefaultSize));
        _items.Columns = columns;

        for (int i = 0; i < category.Items.Count; i++)
        {
            int id = i;
            var control = _items.AddChild(new CraftItemControl(category.Items[i]));
            control.Hovered += c =>
            {
                CraftingGUI.PlayHover(id);
                ShowDetails(c);
            };
            control.Clicked += c =>
            {
                if (c.Data?.blueprint != null) CraftingGUI.Craft(c.Data.blueprint);
            };
            control.Refresh();
            _itemControls.Add(control);
        }

        _categories.Visible = false;
        _itemsView.Visible = true;
        _detailsFor = null;
        if (_itemControls.Count > 0) ShowDetails(_itemControls[0]);
        else ClearDetails();
    }

    private void ShowDetails(CraftItemControl control)
    {
        if (ReferenceEquals(control, _detailsFor) && _ingredients.Children.Count > 0) return;
        _detailsFor = control;
        var data = control.Data;
        var item = data.item;
        int quantity = data.blueprint?.Product?.Quantity ?? 1;
        string name = quantity > 1 ? $"{item.Name} ({quantity})" : item.Name;
        if (_detailsName.Text != name) _detailsName.Text = name;
        string description = item.Description ?? "";
        if (_detailsDescription.Text != description) _detailsDescription.Text = description;
        _detailsDescription.Visible = description.Length > 0;

        _ingredients.ClearChildren();
        _ingredientLabels.Clear();
        if (data.blueprint?.Ingredients == null)
        {
            _ingredients.AddChild(StatusText.Create("Not craftable", StatusKind.Bad));
            return;
        }

        foreach (var ingredient in data.blueprint.Ingredients)
        {
            var row = _ingredients.AddChild(new HBoxContainer());
            row.AddThemeConstantOverride("separation", 4f);
            var icon = row.AddChild(new ItemIconControl(IngredientIconSize));
            icon.Set(ingredient.Item.Icon, 0, ingredient.Item.Name);
            var label = row.AddChild(StatusText.Create(ingredient.ToString(), KindOf(ingredient)));
            _ingredientLabels.Add((label, ingredient));
        }
    }

    private static StatusKind KindOf(Ingredient ingredient) => CraftingGUI.AvailableCount(ingredient) >= ingredient.Quantity ? StatusKind.Good : StatusKind.Bad;

    private void ClearDetails()
    {
        _detailsName.Text = "";
        _detailsDescription.Text = "";
        _ingredients.ClearChildren();
        _ingredientLabels.Clear();
    }
}
