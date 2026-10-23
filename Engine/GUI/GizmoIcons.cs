using ImGuiNET;
using OpenTK.Mathematics;

namespace Engine.GUI;

public enum GizmoIcon : byte
{
    CenterOfMass,
    GeometryCenter,
    BlockLight,
    Camera,
    Particles,
    PointLight,
    SpotLight,
    Thruster,
    Cannon,
    SpawnPoint,
    Audio
}

public static class GizmoIcons
{
    public const float MaxDistance = 150f;
    public const float NearSize = 14f;
    public const float FarSize = 7f;

    private readonly struct Entry
    {
        public readonly Vector3 World;
        public readonly GizmoIcon Icon;
        public readonly uint Color;
        public readonly string Label;

        public Entry(Vector3 world, GizmoIcon icon, uint color, string label)
        {
            World = world;
            Icon = icon;
            Color = color;
            Label = label;
        }
    }

    private static readonly List<Entry> _entries = new List<Entry>();

    public static int Count => _entries.Count;

    public static float SizeAt(float distance)
    {
        float t = Math.Clamp(distance / MaxDistance, 0f, 1f);
        return NearSize + (FarSize - NearSize) * t;
    }

    public static uint Pack(Color4 color)
    {
        uint r = (uint)(Math.Clamp(color.R, 0f, 1f) * 255f);
        uint g = (uint)(Math.Clamp(color.G, 0f, 1f) * 255f);
        uint b = (uint)(Math.Clamp(color.B, 0f, 1f) * 255f);
        uint a = (uint)(Math.Clamp(color.A, 0f, 1f) * 255f);
        return (a << 24) | (b << 16) | (g << 8) | r;
    }

    public static bool IsInRange(Vector3 world)
    {
        var camera = Camera.Main;
        return VisualDebug.Enabled && camera != null && (world - camera.Position).LengthSquared <= MaxDistance * MaxDistance;
    }

    public static void Add(Vector3 world, GizmoIcon icon, Color4 color, string label = null)
    {
        if (!IsInRange(world)) return;
        _entries.Add(new Entry(world, icon, Pack(color), label));
    }

    public static void Draw()
    {
        var camera = Camera.Main;
        if (camera == null || !VisualDebug.Enabled)
        {
            _entries.Clear();
            return;
        }

        var drawList = ImGui.GetBackgroundDrawList();
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            var screen = camera.WorldToScreenPoint(entry.World);
            if (screen == Vector2.Zero) continue;

            float size = SizeAt((entry.World - camera.Position).Length);
            var center = new System.Numerics.Vector2(screen.X, screen.Y);
            DrawIcon(drawList, center, size, entry.Icon, entry.Color);
            if (!string.IsNullOrEmpty(entry.Label))
                drawList.AddText(new System.Numerics.Vector2(center.X + size * 1.2f, center.Y - size * 0.5f), entry.Color, entry.Label);
        }

        _entries.Clear();
    }

    public static void Clear()
    {
        _entries.Clear();
    }

    private static void DrawIcon(ImDrawListPtr drawList, System.Numerics.Vector2 c, float s, GizmoIcon icon, uint color)
    {
        uint shadow = 0xAA000000;
        drawList.AddCircleFilled(c, s * 1.05f, 0x66000000, 20);

        switch (icon)
        {
            case GizmoIcon.CenterOfMass:
                drawList.AddCircle(c, s * 0.8f, color, 20, 2f);
                Quadrant(drawList, c, s * 0.8f, 0f, color);
                Quadrant(drawList, c, s * 0.8f, MathF.PI, color);
                break;
            case GizmoIcon.GeometryCenter:
                drawList.AddRect(new System.Numerics.Vector2(c.X - s * 0.7f, c.Y - s * 0.7f), new System.Numerics.Vector2(c.X + s * 0.7f, c.Y + s * 0.7f), color, 0f, ImDrawFlags.None, 2f);
                drawList.AddLine(new System.Numerics.Vector2(c.X - s, c.Y), new System.Numerics.Vector2(c.X + s, c.Y), color, 1.5f);
                drawList.AddLine(new System.Numerics.Vector2(c.X, c.Y - s), new System.Numerics.Vector2(c.X, c.Y + s), color, 1.5f);
                break;
            case GizmoIcon.BlockLight:
                drawList.AddCircleFilled(new System.Numerics.Vector2(c.X, c.Y - s * 0.15f), s * 0.5f, color, 16);
                drawList.AddRectFilled(new System.Numerics.Vector2(c.X - s * 0.25f, c.Y + s * 0.3f), new System.Numerics.Vector2(c.X + s * 0.25f, c.Y + s * 0.7f), color);
                drawList.AddCircle(new System.Numerics.Vector2(c.X, c.Y - s * 0.15f), s * 0.5f, shadow, 16, 1.5f);
                break;
            case GizmoIcon.Camera:
                drawList.AddRectFilled(new System.Numerics.Vector2(c.X - s * 0.7f, c.Y - s * 0.45f), new System.Numerics.Vector2(c.X + s * 0.25f, c.Y + s * 0.45f), color, 2f);
                drawList.AddTriangleFilled(new System.Numerics.Vector2(c.X + s * 0.25f, c.Y), new System.Numerics.Vector2(c.X + s * 0.8f, c.Y - s * 0.45f), new System.Numerics.Vector2(c.X + s * 0.8f, c.Y + s * 0.45f), color);
                drawList.AddCircleFilled(new System.Numerics.Vector2(c.X - s * 0.22f, c.Y), s * 0.22f, shadow, 12);
                break;
            case GizmoIcon.Particles:
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * MathF.PI / 3f;
                    drawList.AddCircleFilled(new System.Numerics.Vector2(c.X + MathF.Cos(angle) * s * 0.6f, c.Y + MathF.Sin(angle) * s * 0.6f), s * 0.16f, color, 8);
                }
                drawList.AddCircleFilled(c, s * 0.22f, color, 10);
                break;
            case GizmoIcon.PointLight:
            case GizmoIcon.SpotLight:
                drawList.AddCircleFilled(c, s * 0.38f, color, 16);
                int rays = icon == GizmoIcon.PointLight ? 8 : 3;
                for (int i = 0; i < rays; i++)
                {
                    float angle = icon == GizmoIcon.PointLight ? i * MathF.PI / 4f : MathF.PI * 0.5f + (i - 1) * 0.45f;
                    var dir = new System.Numerics.Vector2(MathF.Cos(angle), MathF.Sin(angle));
                    drawList.AddLine(c + dir * s * 0.55f, c + dir * s * 0.9f, color, 2f);
                }
                break;
            case GizmoIcon.Thruster:
                drawList.AddTriangleFilled(new System.Numerics.Vector2(c.X, c.Y + s * 0.85f), new System.Numerics.Vector2(c.X - s * 0.5f, c.Y - s * 0.3f), new System.Numerics.Vector2(c.X + s * 0.5f, c.Y - s * 0.3f), color);
                drawList.AddRectFilled(new System.Numerics.Vector2(c.X - s * 0.35f, c.Y - s * 0.8f), new System.Numerics.Vector2(c.X + s * 0.35f, c.Y - s * 0.35f), color);
                break;
            case GizmoIcon.Cannon:
                drawList.AddCircle(c, s * 0.7f, color, 16, 2f);
                drawList.AddLine(new System.Numerics.Vector2(c.X - s, c.Y), new System.Numerics.Vector2(c.X + s, c.Y), color, 2f);
                drawList.AddLine(new System.Numerics.Vector2(c.X, c.Y - s), new System.Numerics.Vector2(c.X, c.Y + s), color, 2f);
                break;
            case GizmoIcon.Audio:
                drawList.AddRectFilled(new System.Numerics.Vector2(c.X - s * 0.75f, c.Y - s * 0.25f), new System.Numerics.Vector2(c.X - s * 0.35f, c.Y + s * 0.25f), color);
                drawList.AddTriangleFilled(new System.Numerics.Vector2(c.X - s * 0.35f, c.Y - s * 0.25f), new System.Numerics.Vector2(c.X + s * 0.1f, c.Y - s * 0.65f), new System.Numerics.Vector2(c.X + s * 0.1f, c.Y + s * 0.65f), color);
                drawList.AddTriangleFilled(new System.Numerics.Vector2(c.X - s * 0.35f, c.Y + s * 0.25f), new System.Numerics.Vector2(c.X - s * 0.35f, c.Y - s * 0.25f), new System.Numerics.Vector2(c.X + s * 0.1f, c.Y + s * 0.65f), color);
                drawList.PathArcTo(c, s * 0.45f, -0.7f, 0.7f, 8);
                drawList.PathStroke(color, ImDrawFlags.None, 1.5f);
                drawList.PathArcTo(c, s * 0.75f, -0.7f, 0.7f, 10);
                drawList.PathStroke(color, ImDrawFlags.None, 1.5f);
                break;
            case GizmoIcon.SpawnPoint:
                drawList.AddLine(new System.Numerics.Vector2(c.X - s * 0.4f, c.Y + s * 0.85f), new System.Numerics.Vector2(c.X - s * 0.4f, c.Y - s * 0.85f), color, 2f);
                drawList.AddTriangleFilled(new System.Numerics.Vector2(c.X - s * 0.4f, c.Y - s * 0.85f), new System.Numerics.Vector2(c.X + s * 0.7f, c.Y - s * 0.45f), new System.Numerics.Vector2(c.X - s * 0.4f, c.Y - s * 0.05f), color);
                break;
        }
    }

    private static void Quadrant(ImDrawListPtr drawList, System.Numerics.Vector2 center, float radius, float start, uint color)
    {
        drawList.PathLineTo(center);
        drawList.PathArcTo(center, radius, start, start + MathF.PI * 0.5f, 10);
        drawList.PathFillConvex(color);
    }
}
