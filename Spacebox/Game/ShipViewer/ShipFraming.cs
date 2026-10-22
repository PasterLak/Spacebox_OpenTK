using OpenTK.Mathematics;

namespace Spacebox.Game.ShipViewer;

public readonly struct ShipFraming
{
    public const float MinRadius = 1f;

    public ShipFraming(Vector3 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    public Vector3 Center { get; }
    public float Radius { get; }

    public static ShipFraming FromStats(ShipBlockStats stats)
    {
        return stats.IsEmpty ? new ShipFraming(Vector3.Zero, MinRadius) : FromBounds(stats.Min, stats.Max);
    }

    public static ShipFraming FromBounds(Vector3 min, Vector3 max)
    {
        var center = (min + max) * 0.5f;
        float radius = MathF.Max(MinRadius, (max - min).Length * 0.5f);
        return new ShipFraming(center, radius);
    }
}
