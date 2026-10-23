using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Oxygen;
using Spacebox.Game.GUI.Canvas.Items;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class OxygenGeneratorWindow : GameWindow
{
    private static readonly Color4 WaterColor = new Color4(0.15f, 0.55f, 1f, 1f);

    private readonly TankGauge _tank = new TankGauge(OxygenGeneratorUI.TankSteps, 120f, WaterColor);
    private readonly ItemSlotControl _slot = new ItemSlotControl();
    private readonly Label _tankText = new Label();
    private readonly Label _status = StatusText.Create();
    private readonly Label _detail = StatusText.Create("", StatusKind.Muted);
    private OxygenGeneratorBlock _shown;

    private readonly OxygenGeneratorUI _oxygenGenerator;

    public OxygenGeneratorWindow(OxygenGeneratorUI oxygenGenerator) : base("Oxygen Generator")
    {
        _oxygenGenerator = oxygenGenerator;
        Name = "OxygenGenerator";
        MatchInventorySize();
        var row = Body.AddChild(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 12f);
        row.AddChild(_tank);
        var info = row.AddChild(new VBoxContainer { CustomMinimumSize = new Vector2(150f, 0f) });
        info.AddChild(_slot);
        info.AddChild(_tankText);
        info.AddChild(_status);
        info.AddChild(_detail);
        _slot.Clicked += _oxygenGenerator.OnSlotClicked;
    }

    public override bool IsOpen => _oxygenGenerator.IsOpen;

    protected override void Refresh()
    {
        var block = _oxygenGenerator.Block;
        if (TitleLabel.Text != _oxygenGenerator.BlockName) TitleLabel.Text = _oxygenGenerator.BlockName;
        if (!ReferenceEquals(block, _shown))
        {
            _shown = block;
            _slot.Bind(_oxygenGenerator.WaterSlot, _oxygenGenerator.WaterIcon);
        }

        _slot.Refresh();
        ShowEnergy(block.ConsumptionRate);
        _tank.Capacity = block.TankCapacity;
        _tank.Value = block.Water;
        string tank = $"Tank: {block.Water}/{block.TankCapacity}";
        if (_tankText.Text != tank) _tankText.Text = tank;
        var (text, kind, detail) = _oxygenGenerator.Status();
        StatusText.Set(_status, text, kind);
        StatusText.Set(_detail, detail, StatusKind.Muted);
        _detail.Visible = detail.Length > 0;
    }
}
