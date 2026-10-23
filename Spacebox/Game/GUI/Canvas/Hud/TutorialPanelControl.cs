using Engine.CanvasUI;
using Spacebox.Game.Tutorial.UI;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class TutorialPanelControl : HudOverlayControl
{
    private const float BaseSize = 12f;

    private readonly TutorialCanvas _canvas = new TutorialCanvas();

    protected override bool IsShown() => TutorialUI.IsShown;

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        _canvas.Begin(renderer, GetThemeFont());
        TutorialUI.Draw(_canvas, new System.Numerics.Vector2(LocalRect.Width, LocalRect.Height), BaseSize);
    }
}
