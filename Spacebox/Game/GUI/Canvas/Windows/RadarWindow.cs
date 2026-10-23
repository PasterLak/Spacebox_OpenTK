using Spacebox.Game.GUI.Canvas.Items;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class RadarWindow : GameWindow
{
    private const float DisplaySize = 260f;

    private readonly RadarDisplay _display = new RadarDisplay(DisplaySize);

    private readonly GameUi _ui;

    public RadarWindow(GameUi ui) : base("Radar")
    {
        _ui = ui;
        Name = "Radar";
        Body.AddChild(_display);
    }

    public override bool IsOpen => _ui.Radar != null && _ui.Radar.IsOpen;

    protected override void Refresh()
    {
        _display.Radar = _ui.Radar;
        ShowEnergy(_ui.Radar?.Block?.ConsumptionRate ?? 0);
    }
}
