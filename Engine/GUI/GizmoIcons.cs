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

public readonly struct GizmoIconEntry
{
    public readonly Vector3 World;
    public readonly GizmoIcon Icon;
    public readonly Color4 Color;
    public readonly string Label;

    public GizmoIconEntry(Vector3 world, GizmoIcon icon, Color4 color, string label)
    {
        World = world;
        Icon = icon;
        Color = color;
        Label = label;
    }
}

public static class GizmoIcons
{
    public const float MaxDistance = 150f;
    public const float NearSize = 14f;
    public const float FarSize = 7f;

    private static readonly List<GizmoIconEntry> _entries = new List<GizmoIconEntry>();

    public static int Count => _entries.Count;

    public static IReadOnlyList<GizmoIconEntry> Entries => _entries;

    public static float SizeAt(float distance)
    {
        float t = Math.Clamp(distance / MaxDistance, 0f, 1f);
        return NearSize + (FarSize - NearSize) * t;
    }

    public static bool IsInRange(Vector3 world)
    {
        var camera = Camera.Main;
        return VisualDebug.Enabled && camera != null && (world - camera.Position).LengthSquared <= MaxDistance * MaxDistance;
    }

    public static void Add(Vector3 world, GizmoIcon icon, Color4 color, string label = null)
    {
        if (!IsInRange(world)) return;
        _entries.Add(new GizmoIconEntry(world, icon, color, label));
    }

    public static void Clear()
    {
        _entries.Clear();
    }
}
