using Engine.CanvasUI;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class FadeOverlayView : GameScreen
{
    public FadeOverlayView()
    {
        Name = "Fade";
    }

    public override bool IsShown => ColorOverlay.IsVisible && !WorldLoadingScreen.IsActive;

    protected override void Draw(ICanvasRenderer renderer)
    {
        var color = ColorOverlay.CurrentColor;
        if (color.A > 0f) renderer.DrawRect(LocalRect, color);
    }
}
