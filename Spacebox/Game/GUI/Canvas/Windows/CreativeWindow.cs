using Engine.CanvasUI;
using Engine.Graphics.Atlas;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Canvas.Items;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class CreativeWindow : GameWindow
{
    private const int VisibleRows = 7;
    private const float SearchWidth = 150f;

    private readonly LineEdit _search = new LineEdit { ThemeTypeVariation = "HeaderField", PlaceholderText = "Search items", MaxLength = 64, CustomMinimumSize = new Vector2(SearchWidth, 0f), SizeFlagsVertical = SizeFlags.ShrinkCenter, TooltipText = "Name, id or category; case and word order do not matter" };
    private readonly VBoxContainer _categories = new VBoxContainer();

    private readonly CreativeWindowUI _creative;
    private readonly List<(CategoryTile Tile, Storage Storage)> _tiles = new List<(CategoryTile, Storage)>();
    private readonly StorageGridControl _grid = new StorageGridControl(ConfigureSlot);
    private readonly Label _nothing = new Label { ThemeTypeVariation = "MutedLabel", Visible = false };
    private CreativeWindowCategory[] _builtFor;

    public CreativeWindow(CreativeWindowUI creative) : base("Creative")
    {
        _creative = creative;
        Name = "Creative";
        Header.AddChild(_search);
        _search.TextChanged += _creative.Search;
        _categories.AddThemeConstantOverride("separation", 0f);
        _grid.SlotClicked += _creative.OnSlotClicked;
        _grid.SlotHovered += slot => _creative.PlayHoverSound(slot.SlotId);

        float height = VisibleRows * ItemSlotControl.DefaultSize;
        var row = Body.AddChild(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 6f);
        var categoryScroll = row.AddChild(new ScrollContainer { CustomMinimumSize = new Vector2(ItemSlotControl.DefaultSize + 12f, height) });
        categoryScroll.AddChild(_categories);

        var itemScroll = row.AddChild(new ScrollContainer { CustomMinimumSize = new Vector2(CreativeWindowUI.Columns * ItemSlotControl.DefaultSize + 12f, height) });
        var items = itemScroll.AddChild(new VBoxContainer());
        items.AddChild(_nothing);
        items.AddChild(_grid);
    }

    public override Sounds.UiSoundSpec OpenSound => default;

    public override Sounds.UiSoundSpec CloseSound => default;

    public override bool IsOpen => _creative.IsVisible;

    protected override void Refresh()
    {
        if (!ReferenceEquals(_builtFor, _creative.Categories)) BuildCategories();
        if (_search.Text != _creative.SearchText && !_search.HasFocus) _search.Text = _creative.SearchText;

        var selected = _creative.SelectedStorage;
        foreach (var (tile, storage) in _tiles) tile.Selected = ReferenceEquals(storage, selected) && !_creative.IsSearching;

        bool nothing = _creative.HasNoResults;
        _nothing.Visible = nothing;
        if (nothing) _nothing.Text = $"Nothing found for \"{_creative.AppliedSearch}\"";
        _grid.Visible = !nothing;
        _grid.Show(_creative.ShownStorage);
    }

    private void BuildCategories()
    {
        _builtFor = _creative.Categories;
        _categories.ClearChildren();
        _tiles.Clear();
        if (_builtFor == null) return;

        AddTile(new CategoryTile(AtlasSprite.None, "ALL\nTYPES", $"Show all items ({_creative.TotalItems})"), _creative.AllItems);
        foreach (var category in _builtFor)
        {
            if (category.Storage == null) continue;
            var icon = category.Items.Count > 0 ? category.Items[0].Icon : AtlasSprite.None;
            AddTile(new CategoryTile(icon, category.Name.Name, $"Show {category.Name.Name} ({category.ItemsCount})"), category.Storage);
        }
    }

    private void AddTile(CategoryTile tile, Storage storage)
    {
        tile.Clicked += () =>
        {
            _search.Text = "";
            _creative.Select(storage);
        };
        _tiles.Add((tile, storage));
        _categories.AddChild(tile);
    }

    private static void ConfigureSlot(ItemSlotControl slot)
    {
        slot.AllowsDrag = false;
        slot.AllowsSplit = false;
        slot.AcceptsDrops = false;
        slot.ShowsStackSize = true;
        slot.ShowsId = true;
    }
}
