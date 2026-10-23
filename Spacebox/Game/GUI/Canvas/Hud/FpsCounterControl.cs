using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class FpsCounterControl : HudOverlayControl
{
    private const float Margin = 8f;
    private static readonly Color4 Color = new Color4(1f, 1f, 1f, 0.9f);

    private int _shownFps = -1;
    private string _text = "";

    protected override bool IsShown() => Settings.Graphics.ShowFps;

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        int fps = Time.FPS;
        if (fps != _shownFps)
        {
            _shownFps = fps;
            _text = fps > 0 ? $"{fps} FPS  {1000f / fps:0.0} ms" : "-- FPS";
        }

        DrawText(renderer, new Vector2(LocalRect.Width - Measure(_text).X - Margin, Margin), _text, Color);
    }
}
