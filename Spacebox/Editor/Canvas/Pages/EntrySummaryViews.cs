using System.Text.Json.Nodes;
using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Editor.Data;
using Spacebox.Game;
using Spacebox.Game.Disassembly;
using Spacebox.Game.GUI;

namespace Spacebox.Editor.Canvas.Pages;

public abstract class RefreshingEntryView : EntryView
{
    private const float CheckInterval = 0.25f;

    private string _signature;
    private float _sinceCheck;

    protected EditorEntry Entry { get; private set; }

    public override void Show(EditorContext editor, EditorEntry entry)
    {
        Entry = entry;
        _signature = null;
        Refresh(editor);
    }

    public override void Tick(EditorContext editor, float deltaTime)
    {
        _sinceCheck += deltaTime;
        if (_sinceCheck < CheckInterval) return;
        _sinceCheck = 0f;
        Refresh(editor);
    }

    private void Refresh(EditorContext editor)
    {
        if (Entry?.Node == null) return;
        string signature = editor.Assets.Version + "|" + Signature(Entry);
        if (signature == _signature) return;
        _signature = signature;
        Rebuild(editor, Entry);
    }

    protected virtual string Signature(EditorEntry entry) => entry.Node.ToJsonString();

    protected abstract void Rebuild(EditorContext editor, EditorEntry entry);

    protected static void AddStack(Container parent, EditorContext editor, string itemId, string quantity)
    {
        var icon = parent.AddChild(CanvasIcons.Image(editor.Assets.ItemIcon(itemId), 24f));
        bool known = editor.Assets.TryGetItem(itemId, out var item);
        icon.TooltipText = known ? $"{item.Name} ({itemId})" : itemId;
        parent.AddChild(new Label(known ? quantity : quantity + " (unknown item)")
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontColor = known ? EditorCanvasTheme.Text : EditorCanvasTheme.Error,
            CustomMinimumSize = new Vector2(0f, 24f)
        });
    }

    protected static Label Dim(string text) => new Label(text) { FontColor = EditorCanvasTheme.TextDim, VerticalAlignment = VerticalAlignment.Center, CustomMinimumSize = new Vector2(0f, 24f) };
}

public sealed class RecipeSummaryView : RefreshingEntryView
{
    private readonly HFlowContainer _row = new HFlowContainer();

    protected override Control Create() => _row;

    protected override void Rebuild(EditorContext editor, EditorEntry entry)
    {
        _row.ClearChildren();
        if (entry.Node is not JsonObject obj) return;

        if (EditorJson.Get(obj, "Ingredients") is JsonArray ingredients)
        {
            foreach (var ingredient in ingredients) Stack(editor, ingredient as JsonObject);
        }
        else
        {
            Stack(editor, EditorJson.Get(obj, "Ingredient") as JsonObject);
        }

        _row.AddChild(Dim("  ->  "));
        Stack(editor, EditorJson.Get(obj, "Product") as JsonObject);

        double duration = EditorJson.GetNumber(obj, "DurationSec", -1);
        if (duration >= 0) _row.AddChild(Dim($"   {duration:0.##}s"));
    }

    private void Stack(EditorContext editor, JsonObject stack)
    {
        if (stack == null) return;
        AddStack(_row, editor, EditorJson.GetString(stack, "Item"), $"x{EditorJson.GetNumber(stack, "Quantity", 1):0}");
    }
}

public sealed class DisassemblySummaryView : RefreshingEntryView
{
    private readonly HFlowContainer _row = new HFlowContainer();

    protected override Control Create() => _row;

    protected override void Rebuild(EditorContext editor, EditorEntry entry)
    {
        _row.ClearChildren();
        if (entry.Node is not JsonObject obj) return;

        string id = EditorJson.GetString(obj, "Item");
        if (!editor.Assets.TryGetItem(id, out var item))
        {
            _row.AddChild(Dim("Pick an item to see what the disassembler gives back."));
            return;
        }

        if (!DisassemblyRecipes.TryGet(item, out var recipe))
        {
            _row.AddChild(Dim($"{item.Name} cannot be disassembled (last loaded state)."));
            return;
        }

        AddStack(_row, editor, id, $"x{recipe.InputQuantity}");
        _row.AddChild(Dim("  ->  "));
        foreach (var product in recipe.Products)
        {
            var icon = _row.AddChild(CanvasIcons.Image(product.Item.Icon, 24f));
            icon.TooltipText = product.Item.Name;
            _row.AddChild(new Label($"x{product.Quantity}") { VerticalAlignment = VerticalAlignment.Center, CustomMinimumSize = new Vector2(0f, 24f) });
        }
        _row.AddChild(Dim($"   {Time.TicksToSeconds(recipe.DurationTicks):0.##}s (last loaded state)"));
    }
}

public sealed class CraftingCategoryView : RefreshingEntryView
{
    public const int Rows = 2;

    private readonly VBoxContainer _root = new VBoxContainer();
    private readonly GridContainer _grid = new GridContainer();
    private readonly HFlowContainer _items = new HFlowContainer();
    private readonly Label _itemsTitle = new Label { FontColor = EditorCanvasTheme.TextDim };

    protected override Control Create()
    {
        _root.AddChild(new Label("Crafting window (categories as the game arranges them):") { FontColor = EditorCanvasTheme.TextDim });
        _root.AddChild(_grid);
        _root.AddChild(_itemsTitle);
        _root.AddChild(_items);
        return _root;
    }

    protected override string Signature(EditorEntry entry) => entry.File.Root?.ToJsonString() + "|" + (entry.Node as JsonObject)?["Id"];

    protected override void Rebuild(EditorContext editor, EditorEntry entry)
    {
        _grid.ClearChildren();
        _items.ClearChildren();
        if (entry.Node is not JsonObject selected) return;

        var categories = ReadCategories(entry);
        string selectedId = EditorJson.GetString(selected, "Id");
        var cells = CraftingCategoryLayout.Arrange(categories, Rows, out int columns);
        _grid.Columns = Math.Max(1, columns);
        foreach (var category in cells) _grid.AddChild(Cell(editor, category, selectedId));
        ShowItems(selectedId);
    }

    private static List<CraftingCategory> ReadCategories(EditorEntry entry)
    {
        var result = new List<CraftingCategory>();
        if (EditorJson.GetArray(entry.File.Root, "Categories") is not JsonArray array) return result;

        foreach (var node in array)
        {
            if (node is not JsonObject obj) continue;
            result.Add(new CraftingCategory
            {
                Id = EditorJson.GetString(obj, "Id"),
                Name = EditorJson.GetString(obj, "Name"),
                Icon = EditorJson.GetString(obj, "Icon"),
                Row = (int)EditorJson.GetNumber(obj, "Row", 0)
            });
        }
        return result;
    }

    private static Control Cell(EditorContext editor, CraftingCategory category, string selectedId)
    {
        bool isSelected = category != null && string.Equals(category.Id, selectedId, StringComparison.OrdinalIgnoreCase);
        var color = category == null ? new Color4(1f, 1f, 1f, 0.08f) : isSelected ? EditorCanvasTheme.AccentDark : EditorCanvasTheme.Field;
        var cell = new ColorRect { Color = color, CustomMinimumSize = new Vector2(84f, 52f) };
        if (category == null) return cell;

        cell.MouseFilter = MouseFilter.Stop;
        cell.TooltipText = $"{category.Name} ({category.Id})\nRow {(category.Row == 0 ? "auto" : category.Row.ToString())}, icon {category.Icon}";
        var icon = cell.AddChild(CanvasIcons.Image(editor.Assets.ItemIcon(category.Icon), 32f));
        icon.MouseFilter = MouseFilter.Ignore;
        icon.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight, 4f, new Vector2(32f, 32f));
        cell.AddChild(new Label(category.Name) { Position = new Vector2(4f, 2f), MouseFilter = MouseFilter.Ignore });
        return cell;
    }

    private void ShowItems(string categoryId)
    {
        if (string.IsNullOrEmpty(categoryId) || !GameAssets.CraftingCategories.TryGetValue(categoryId, out var loaded))
        {
            _itemsTitle.Text = "No craftable items in this category (or not loaded yet: Save & Reload).";
            return;
        }

        _itemsTitle.Text = $"{loaded.Items.Count} craftable items in this category (last loaded state):";
        foreach (var data in loaded.Items)
        {
            if (data.item == null) continue;
            var icon = _items.AddChild(CanvasIcons.Image(data.item.Icon, 28f));
            icon.TooltipText = data.item.Name;
        }
    }
}
