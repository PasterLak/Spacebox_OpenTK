using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
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
    private ResourceProcessingBlock _shownBlock;

    public ProcessingWindow() : base("Machine")
    {
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
        _input.Clicked += ResourceProcessingGUI.MoveItems;
        _output.Clicked += ResourceProcessingGUI.MoveItems;

        row.AddChild(new VSeparator());
        var right = row.AddChild(new VBoxContainer());
        right.AddChild(new Label { Text = "Recipes", ThemeTypeVariation = "SectionLabel" });
        var scroll = right.AddChild(new ScrollContainer { CustomMinimumSize = new Vector2(RecipeIconSize * 3f + 40f, RecipeListHeight) });
        scroll.AddChild(_recipes);
        _recipes.AddThemeConstantOverride("separation", 4f);
    }

    public override bool IsOpen => ResourceProcessingGUI.IsOpen;

    public override bool LacksPower => ResourceProcessingGUI.Block is { RequiresPower: true, HasPower: false };

    protected override void Refresh()
    {
        if (TitleLabel.Text != ResourceProcessingGUI.WindowName) TitleLabel.Text = ResourceProcessingGUI.WindowName;
        if (ResourceProcessingGUI.Block.HasPower) StatusText.Set(_status, ResourceProcessingGUI.Status, StatusKind.Normal);
        else StatusText.Set(_status, "NO POWER", StatusKind.Bad);
        _arrow.Progress = ResourceProcessingGUI.Progress;
        ShowEnergy(ResourceProcessingGUI.Block.ConsumptionRate);

        if (!ReferenceEquals(_shownBlock, ResourceProcessingGUI.Block))
        {
            _shownBlock = ResourceProcessingGUI.Block;
            _input.Bind(ResourceProcessingGUI.InputSlot, ResourceProcessingGUI.InputIcon);
            _output.Bind(ResourceProcessingGUI.OutputSlot, ResourceProcessingGUI.OutputIcon);
            BuildRecipes();
        }

        _input.Refresh();
        _output.Refresh();
    }

    private void BuildRecipes()
    {
        _recipes.ClearChildren();
        foreach (var recipe in ResourceProcessingGUI.Recipes)
        {
            if (recipe?.Ingredient.Item == null || recipe.Product.Item == null) continue;

            var row = _recipes.AddChild(new HBoxContainer());
            row.AddThemeConstantOverride("separation", 6f);
            var input = row.AddChild(new ItemIconControl(RecipeIconSize));
            input.Set(recipe.Ingredient.Item.Icon, recipe.Ingredient.Quantity, recipe.Ingredient.Item.Name);
            row.AddChild(new Label { Text = "->", VerticalAlignment = VerticalAlignment.Center, SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var output = row.AddChild(new ItemIconControl(RecipeIconSize));
            output.Set(recipe.Product.Item.Icon, recipe.Product.Quantity, recipe.Product.Item.Name, $"Time: {ResourceProcessingGUI.RecipeSeconds(recipe):0.#}s");
        }
    }
}
