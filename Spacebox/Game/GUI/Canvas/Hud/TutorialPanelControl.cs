using Engine.CanvasUI;
using Spacebox.Game.Tutorial.UI;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class TutorialPanelControl : HudOverlayControl
{
    private const float BaseSize = 12f;

    private readonly TutorialCanvas _canvas = new TutorialCanvas();

    private readonly TutorialUI _tutorial;

    public TutorialPanelControl(TutorialUI tutorial)
    {
        _tutorial = tutorial;
    }

    protected override bool IsShown() => _tutorial.IsShown;

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        _canvas.Begin(renderer, GetThemeFont());
        _tutorial.Draw(_canvas, new System.Numerics.Vector2(LocalRect.Width, LocalRect.Height), BaseSize);
    }
}
