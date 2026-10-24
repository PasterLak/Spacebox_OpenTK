using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.GUI;

public static class GizmoIconPainter
{
    private static readonly Color4 Backdrop = new Color4(0f, 0f, 0f, 0.4f);
    private static readonly Color4 Shadow = new Color4(0f, 0f, 0f, 0.67f);

    public static void Draw(ICanvasRenderer renderer, Vector2 c, float s, GizmoIcon icon, Color4 color)
    {
        CanvasShapes.FilledCircle(renderer, c, s * 1.05f, Backdrop);

        switch (icon)
        {
            case GizmoIcon.CenterOfMass:
                CanvasShapes.Circle(renderer, c, s * 0.8f, color, 2f, 20);
                CanvasShapes.FilledSector(renderer, c, s * 0.8f, 0f, MathF.PI * 0.5f, color);
                CanvasShapes.FilledSector(renderer, c, s * 0.8f, MathF.PI, MathF.PI * 1.5f, color);
                break;
            case GizmoIcon.GeometryCenter:
                CanvasShapes.RectOutline(renderer, new Rect2(c.X - s * 0.7f, c.Y - s * 0.7f, s * 1.4f, s * 1.4f), color, 2f);
                CanvasShapes.Line(renderer, new Vector2(c.X - s, c.Y), new Vector2(c.X + s, c.Y), color, 1.5f);
                CanvasShapes.Line(renderer, new Vector2(c.X, c.Y - s), new Vector2(c.X, c.Y + s), color, 1.5f);
                break;
            case GizmoIcon.BlockLight:
                CanvasShapes.FilledCircle(renderer, new Vector2(c.X, c.Y - s * 0.15f), s * 0.5f, color);
                renderer.DrawRect(new Rect2(c.X - s * 0.25f, c.Y + s * 0.3f, s * 0.5f, s * 0.4f), color);
                CanvasShapes.Circle(renderer, new Vector2(c.X, c.Y - s * 0.15f), s * 0.5f, Shadow, 1.5f, 16);
                break;
            case GizmoIcon.Camera:
                renderer.DrawRect(new Rect2(c.X - s * 0.7f, c.Y - s * 0.45f, s * 0.95f, s * 0.9f), color);
                CanvasShapes.FilledTriangle(renderer, new Vector2(c.X + s * 0.25f, c.Y), new Vector2(c.X + s * 0.8f, c.Y - s * 0.45f), new Vector2(c.X + s * 0.8f, c.Y + s * 0.45f), color);
                CanvasShapes.FilledCircle(renderer, new Vector2(c.X - s * 0.22f, c.Y), s * 0.22f, Shadow);
                break;
            case GizmoIcon.Particles:
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * MathF.PI / 3f;
                    CanvasShapes.FilledCircle(renderer, new Vector2(c.X + MathF.Cos(angle) * s * 0.6f, c.Y + MathF.Sin(angle) * s * 0.6f), s * 0.16f, color);
                }
                CanvasShapes.FilledCircle(renderer, c, s * 0.22f, color);
                break;
            case GizmoIcon.PointLight:
            case GizmoIcon.SpotLight:
                CanvasShapes.FilledCircle(renderer, c, s * 0.38f, color);
                int rays = icon == GizmoIcon.PointLight ? 8 : 3;
                for (int i = 0; i < rays; i++)
                {
                    float angle = icon == GizmoIcon.PointLight ? i * MathF.PI / 4f : MathF.PI * 0.5f + (i - 1) * 0.45f;
                    var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                    CanvasShapes.Line(renderer, c + direction * s * 0.55f, c + direction * s * 0.9f, color, 2f);
                }
                break;
            case GizmoIcon.Thruster:
                CanvasShapes.FilledTriangle(renderer, new Vector2(c.X, c.Y + s * 0.85f), new Vector2(c.X - s * 0.5f, c.Y - s * 0.3f), new Vector2(c.X + s * 0.5f, c.Y - s * 0.3f), color);
                renderer.DrawRect(new Rect2(c.X - s * 0.35f, c.Y - s * 0.8f, s * 0.7f, s * 0.45f), color);
                break;
            case GizmoIcon.Cannon:
                CanvasShapes.Circle(renderer, c, s * 0.7f, color, 2f, 16);
                CanvasShapes.Line(renderer, new Vector2(c.X - s, c.Y), new Vector2(c.X + s, c.Y), color, 2f);
                CanvasShapes.Line(renderer, new Vector2(c.X, c.Y - s), new Vector2(c.X, c.Y + s), color, 2f);
                break;
            case GizmoIcon.Audio:
                renderer.DrawRect(new Rect2(c.X - s * 0.75f, c.Y - s * 0.25f, s * 0.4f, s * 0.5f), color);
                CanvasShapes.FilledTriangle(renderer, new Vector2(c.X - s * 0.35f, c.Y - s * 0.25f), new Vector2(c.X + s * 0.1f, c.Y - s * 0.65f), new Vector2(c.X + s * 0.1f, c.Y + s * 0.65f), color);
                CanvasShapes.FilledTriangle(renderer, new Vector2(c.X - s * 0.35f, c.Y + s * 0.25f), new Vector2(c.X - s * 0.35f, c.Y - s * 0.25f), new Vector2(c.X + s * 0.1f, c.Y + s * 0.65f), color);
                CanvasShapes.Arc(renderer, c, s * 0.45f, -0.7f, 0.7f, color, 1.5f, 8);
                CanvasShapes.Arc(renderer, c, s * 0.75f, -0.7f, 0.7f, color, 1.5f, 10);
                break;
            case GizmoIcon.SpawnPoint:
                CanvasShapes.Line(renderer, new Vector2(c.X - s * 0.4f, c.Y + s * 0.85f), new Vector2(c.X - s * 0.4f, c.Y - s * 0.85f), color, 2f);
                CanvasShapes.FilledTriangle(renderer, new Vector2(c.X - s * 0.4f, c.Y - s * 0.85f), new Vector2(c.X + s * 0.7f, c.Y - s * 0.45f), new Vector2(c.X - s * 0.4f, c.Y - s * 0.05f), color);
                break;
        }
    }
}
