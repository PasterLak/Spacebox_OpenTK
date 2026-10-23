using Spacebox.Game.GUI.Canvas.Items;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class RadarWindow : GameWindow
{
    private const float DisplaySize = 260f;

    private readonly RadarDisplay _display = new RadarDisplay(DisplaySize);

    public RadarWindow() : base("Radar")
    {
        Name = "Radar";
        Body.AddChild(_display);
    }

    public override bool IsOpen => RadarUI.Instance != null && RadarUI.Instance.IsOpen;

    protected override void Refresh()
    {
        _display.Radar = RadarUI.Instance;
    }
}
