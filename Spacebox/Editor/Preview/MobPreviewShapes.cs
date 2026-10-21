using OpenTK.Mathematics;

namespace Spacebox.Editor.Preview;

public static class MobPreviewShapes
{
    public static List<Vector3> Circle(float radius, float y, int segments)
    {
        segments = Math.Max(3, segments);
        var points = new List<Vector3>(segments + 1);
        for (int i = 0; i <= segments; i++)
        {
            float angle = MathF.Tau * i / segments;
            points.Add(new Vector3(MathF.Cos(angle) * radius, y, MathF.Sin(angle) * radius));
        }
        return points;
    }

    public static List<Vector3> CubeEdges(Vector3 min, float size)
    {
        var max = min + new Vector3(size);
        return new List<Vector3>
        {
            new(min.X, min.Y, min.Z), new(max.X, min.Y, min.Z), new(max.X, min.Y, max.Z), new(min.X, min.Y, max.Z), new(min.X, min.Y, min.Z),
            new(min.X, max.Y, min.Z), new(max.X, max.Y, min.Z), new(max.X, min.Y, min.Z), new(max.X, max.Y, min.Z),
            new(max.X, max.Y, max.Z), new(max.X, min.Y, max.Z), new(max.X, max.Y, max.Z),
            new(min.X, max.Y, max.Z), new(min.X, min.Y, max.Z), new(min.X, max.Y, max.Z), new(min.X, max.Y, min.Z)
        };
    }
}
