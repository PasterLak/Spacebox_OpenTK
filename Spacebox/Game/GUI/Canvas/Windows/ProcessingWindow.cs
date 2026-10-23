using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks.Components;
using Spacebox.Game.GUI.Canvas.Items;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class ProcessingWindow : GameWindow
{
    private const float RecipeIconSize = 26f;
    private const float RecipeListHeight = 112f;

    private readonly Label _status = StatusText.Create();
    private readonly ItemSlotControl _input = new ItemSlotControl();
    private readonly ItemSlotControl _output = new ItemSlotControl();
    private readonly VBoxContainer _recipes = new VBoxContainer();
    private readonly ProgressArrow _arrow = new ProgressArrow(new Vector2(44f, 22f)) { SizeFlagsVertical = SizeFlags.ShrinkCenter };
    private readonly StorageGridControl _buffer = new StorageGridControl { Visible = false, SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
    private ProcessingComponent _shownBlock;

    private readonly ResourceProcessingGUI _processing;

    public ProcessingWindow(ResourceProcessingGUI processing) : base("Machine")
    {
        _processing = processing;
        Name = "Processing";
        MatchInventorySize();
        var row = Body.AddChild(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 10f);

        var left = row.AddChild(new VBoxContainer { CustomMinimumSize = new Vector2(150f, 0f), SizeFlagsHorizontal = SizeFlags.ExpandFill });
        left.AddChild(_status);
        var slots = left.AddChild(new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill, Alignment = BoxAlignment.Center });
        slots.AddThemeConstantOverride("separation", 12f);
        slots.AddChild(_input);
        slots.AddChild(_arrow);
        slots.AddChild(_output);
        _input.Clicked += _processing.MoveItems;
        _output.Clicked += _processing.MoveItems;
        left.AddChild(_buffer);
        _buffer.SlotClicked += _processing.MoveItems;

        row.AddChild(new VSeparator());
        var right = row.AddChild(new VBoxContainer());
        right.AddChild(new Label { Text = "Recipes", ThemeTypeVariation = "SectionLabel" });
        var scroll = right.AddChild(new ScrollContainer { CustomMinimumSize = new Vector2(RecipeIconSize * 3f + 40f, RecipeListHeight) });
        scroll.AddChild(_recipes);
        _recipes.AddThemeConstantOverride("separation", 4f);
    }

    public override bool IsOpen => _processing.IsOpen;

    public override bool LacksPower => _processing.Block is { RequiresPower: true, HasPower: false };

    protected override void Refresh()
    {
        if (TitleLabel.Text != _processing.WindowName) TitleLabel.Text = _processing.WindowName;
        if (_processing.Block.HasPower) StatusText.Set(_status, _processing.Status, StatusKind.Normal);
        else StatusText.Set(_status, "NO POWER", StatusKind.Bad);
        _arrow.Progress = _processing.Progress;
        ShowEnergy(_processing.Block.Host.ConsumptionRate);

        if (!ReferenceEquals(_shownBlock, _processing.Block))
        {
            _shownBlock = _processing.Block;
            _input.Bind(_processing.InputSlot, _processing.InputIcon);
            _output.Bind(_processing.OutputSlot, _processing.OutputIcon);
            BuildRecipes();
        }

        _input.Refresh();
        _output.Refresh();
        _buffer.Visible = _processing.Buffer != null;
        _buffer.Show(_processing.Buffer);
    }

    private void BuildRecipes()
    {
        _recipes.ClearChildren();
        foreach (var recipe in _processing.Recipes)
        {
            if (recipe?.Ingredient.Item == null || recipe.Product.Item == null) continue;

            var row = _recipes.AddChild(new HBoxContainer());
            row.AddThemeConstantOverride("separation", 6f);
            var input = row.AddChild(new ItemIconControl(RecipeIconSize));
            input.Set(recipe.Ingredient.Item.Icon, recipe.Ingredient.Quantity, recipe.Ingredient.Item.Name);
            row.AddChild(new Label { Text = "->", VerticalAlignment = VerticalAlignment.Center, SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var output = row.AddChild(new ItemIconControl(RecipeIconSize));
            output.Set(recipe.Product.Item.Icon, recipe.Product.Quantity, recipe.Product.Item.Name, $"Time: {_processing.RecipeSeconds(recipe):0.#}s");
        }
    }
}
