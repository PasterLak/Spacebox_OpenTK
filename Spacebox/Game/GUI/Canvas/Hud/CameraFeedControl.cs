using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Rendering;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class CameraFeedControl : HudOverlayControl
{
    private const string RecText = "REC";
    private const string CameraText = "CAM 01";
    private const float BlinkPeriod = 1f;
    private static readonly Color4 White = new Color4(1f, 1f, 1f, 0.85f);
    private static readonly Color4 Red = new Color4(1f, 0.15f, 0.1f, 0.95f);

    private int _shownSeconds = -1;
    private string _timer = "";

    protected override bool IsShown() => base.IsShown() && CameraFeedController.IsVisible;

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        var rect = LocalRect;
        float margin = MathF.Round(MathF.Min(rect.Width, rect.Height) * 0.06f);
        float corner = MathF.Round(margin * 0.9f);
        float thickness = MathF.Max(2f, MathF.Round(rect.Height / 400f));
        DrawCorners(renderer, new Vector2(margin), new Vector2(rect.Width - margin, rect.Height - margin), corner, thickness);

        float lineHeight = LineHeight;
        var origin = new Vector2(margin + thickness * 4f, margin + thickness * 3f);
        float seconds = CameraFeedController.ActiveSeconds;
        if (seconds % BlinkPeriod < BlinkPeriod * 0.6f)
        {
            float dot = MathF.Round(lineHeight * 0.7f);
            renderer.DrawRect(new Rect2(origin.X, origin.Y + MathF.Round((lineHeight - dot) * 0.5f), dot, dot), Red);
        }
        DrawText(renderer, origin + new Vector2(lineHeight * 1.1f, 0f), RecText, White);
        DrawText(renderer, new Vector2(rect.Width - margin - thickness * 4f - Measure(CameraText).X, origin.Y), CameraText, White);

        int whole = (int)seconds;
        if (whole != _shownSeconds)
        {
            _shownSeconds = whole;
            _timer = $"{whole / 3600:00}:{whole / 60 % 60:00}:{whole % 60:00}";
        }
        DrawText(renderer, new Vector2(rect.Width - margin - thickness * 4f - Measure(_timer).X, rect.Height - margin - thickness * 3f - lineHeight), _timer, White);
    }

    private static void DrawCorners(ICanvasRenderer renderer, Vector2 min, Vector2 max, float length, float thickness)
    {
        DrawCorner(renderer, min, new Vector2(1f, 1f), length, thickness);
        DrawCorner(renderer, new Vector2(max.X, min.Y), new Vector2(-1f, 1f), length, thickness);
        DrawCorner(renderer, new Vector2(min.X, max.Y), new Vector2(1f, -1f), length, thickness);
        DrawCorner(renderer, max, new Vector2(-1f, -1f), length, thickness);
    }

    private static void DrawCorner(ICanvasRenderer renderer, Vector2 point, Vector2 direction, float length, float thickness)
    {
        float x = direction.X > 0 ? point.X : point.X - length;
        float y = direction.Y > 0 ? point.Y : point.Y - thickness;
        renderer.DrawRect(new Rect2(x, y, length, thickness), White);
        float vx = direction.X > 0 ? point.X : point.X - thickness;
        float vy = direction.Y > 0 ? point.Y : point.Y - length;
        renderer.DrawRect(new Rect2(vx, vy, thickness, length), White);
    }
}
