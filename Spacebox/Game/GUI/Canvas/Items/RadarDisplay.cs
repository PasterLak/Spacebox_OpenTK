using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Radar;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class RadarDisplay : Control
{
    private static readonly Color4 Tint = new Color4(62 / 255f, 150 / 255f, 67 / 255f, 1f);
    private const float PointSize = 3f;
    private static readonly Color4 ShipColor = new Color4(0.45f, 0.85f, 1f, 1f);
    private static readonly Color4 PlayerColor = new Color4(1f, 1f, 1f, 1f);
    private static readonly Color4 MobColor = new Color4(1f, 0.35f, 0.3f, 1f);

    public RadarDisplay(float size)
    {
        CustomMinimumSize = new Vector2(size, size);
        MouseFilter = MouseFilter.Ignore;
    }

    public RadarUI Radar { get; set; }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var radar = Radar;
        if (radar == null) return;

        var rect = LocalRect;
        var center = rect.Size * 0.5f;
        DrawBackground(renderer, radar, rect);

        var blips = radar.Blips;
        for (int i = 0; i < blips.Count; i++)
        {
            var blip = blips[i];
            float distance = rect.Height * 0.5f * RadarMath.Radius(blip.Distance, radar.Range);
            var point = center + blip.Direction * distance;
            var color = ColorOf(blip.Kind);
            renderer.DrawRect(new Rect2(MathF.Round(point.X - PointSize), MathF.Round(point.Y - PointSize), PointSize * 2f, PointSize * 2f), new Color4(color.R, color.G, color.B, blip.Alpha));
        }

        if (radar.IsScanWaveActive)
        {
            var half = rect.Size * 0.5f * radar.ScanWaveProgress;
            DrawTexture(renderer, radar.ScanningTexture, new Rect2(center - half, half * 2f), new Color4(1f, 1f, 1f, Math.Clamp(1f - radar.ScanWaveProgress, 0f, 1f)));
        }

        var previous = renderer.Transform;
        renderer.Transform = previous * Transform2D.Create(center, radar.Angle, Vector2.One);
        DrawTexture(renderer, radar.LineTexture, new Rect2(-center, rect.Size), Color4.White);
        renderer.Transform = previous;

        DrawTexture(renderer, radar.MaskTexture, rect, Color4.White);
    }

    private static void DrawBackground(ICanvasRenderer renderer, RadarUI radar, Rect2 rect)
    {
        var sky = radar.SpaceBackground;
        if (sky != null && radar.ShowsSpaceBackground)
        {
            var position = radar.BackgroundUvPosition;
            var size = radar.BackgroundUvSize;
            renderer.DrawTexture(sky, rect, new Rect2(position.X, position.Y, size.X, size.Y), Color4.White);
        }
        if (!radar.ShowsSpaceBackground) CanvasShapes.FilledCircle(renderer, rect.Position + rect.Size * 0.5f, MathF.Min(rect.Width, rect.Height) * 0.5f, Tint);
        DrawTexture(renderer, radar.GridTexture, rect, Color4.White);
    }

    private static Color4 ColorOf(RadarContactKind kind) => kind switch
    {
        RadarContactKind.Ship => ShipColor,
        RadarContactKind.Player => PlayerColor,
        _ => MobColor
    };

    private static void DrawTexture(ICanvasRenderer renderer, Texture2D texture, Rect2 rect, Color4 color)
    {
        if (texture != null) renderer.DrawTexture(texture, rect, CanvasDraw.UvOf(texture, null), color);
    }
}
