using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Disassembly;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Canvas.Items;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class DisassemblerWindow : GameWindow
{
    private const float PreviewIconSize = 22f;

    private readonly Label _status = StatusText.Create();
    private readonly StorageGridControl _input = new StorageGridControl();
    private readonly StorageGridControl _output = new StorageGridControl();
    private readonly ProgressArrow _arrow = new ProgressArrow(new Vector2(70f, 30f)) { SizeFlagsVertical = SizeFlags.ShrinkCenter };
    private readonly HBoxContainer _preview = new HBoxContainer { CustomMinimumSize = new Vector2(0f, PreviewIconSize) };
    private DisassemblerBlock _shown;
    private DisassemblyRecipe _previewFor;

    private readonly DisassemblerUI _disassembler;

    public DisassemblerWindow(DisassemblerUI disassembler) : base("Disassembler")
    {
        _disassembler = disassembler;
        Name = "Disassembler";
        MatchColumnWidth();
        Body.AddChild(_status);
        var row = Body.AddChild(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 10f);
        row.Alignment = BoxAlignment.Center;
        row.AddChild(_input);
        row.AddChild(_arrow);
        row.AddChild(_output);
        Body.AddChild(_preview);
        _preview.AddThemeConstantOverride("separation", 4f);
        _input.SlotClicked += _disassembler.OnSlotClicked;
        _output.SlotClicked += _disassembler.OnSlotClicked;
    }

    public override bool IsOpen => _disassembler.IsOpen;

    public override bool LacksPower => _disassembler.Block is { HasPower: false };

    protected override void Refresh()
    {
        var block = _disassembler.Block;
        if (TitleLabel.Text != _disassembler.BlockName) TitleLabel.Text = _disassembler.BlockName;
        if (!ReferenceEquals(block, _shown))
        {
            _shown = block;
            _input.Show(block.InputStorage);
            _output.Show(block.OutputStorage);
            foreach (var slot in _input.Slots) slot.Bind(slot.Slot, _disassembler.InputIcon);
            foreach (var slot in _output.Slots) slot.Bind(slot.Slot, _disassembler.OutputIcon);
        }

        _input.Show(block.InputStorage);
        _output.Show(block.OutputStorage);
        ShowEnergy(block.Power);
        var (text, kind) = _disassembler.Status();
        StatusText.Set(_status, text, kind);
        _arrow.Progress = _disassembler.Progress;

        var recipe = _disassembler.PreviewRecipe();
        if (!ReferenceEquals(recipe, _previewFor)) BuildPreview(recipe);
    }

    private void BuildPreview(DisassemblyRecipe recipe)
    {
        _previewFor = recipe;
        _preview.ClearChildren();
        if (recipe == null) return;

        AddStack(recipe.Input, recipe.InputQuantity);
        _preview.AddChild(new Label { Text = "->", VerticalAlignment = VerticalAlignment.Center });
        foreach (var product in recipe.Products) AddStack(product.Item, product.Quantity);
        _preview.AddChild(new Label { Text = $"{Time.TicksToSeconds(recipe.DurationTicks):0.#}s", ThemeTypeVariation = "MutedLabel", VerticalAlignment = VerticalAlignment.Center });
    }

    private void AddStack(Item item, int quantity)
    {
        var icon = _preview.AddChild(new ItemIconControl(PreviewIconSize));
        icon.Set(item.Icon, 0, item.Name);
        _preview.AddChild(new Label { Text = "x" + quantity, VerticalAlignment = VerticalAlignment.Center });
    }
}
