using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public readonly struct HullSample
{
    public readonly Vector3 Point;
    public readonly Vector3 Normal;

    public HullSample(Vector3 point, Vector3 normal)
    {
        Point = point;
        Normal = normal;
    }
}

public sealed class ShipHull
{
    public const float SampleInset = 0.45f;

    private static readonly Vector3i[] Faces =
    {
        new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0),
        new Vector3i(0, 1, 0), new Vector3i(0, -1, 0),
        new Vector3i(0, 0, 1), new Vector3i(0, 0, -1)
    };

    private readonly List<HullSample> _samples = new List<HullSample>();

    public IReadOnlyList<HullSample> Samples => _samples;
    public float Radius { get; private set; }

    public void Clear()
    {
        _samples.Clear();
        Radius = 0f;
    }

    public void AddCell(Vector3i cell, Func<Vector3i, bool> isSolid)
    {
        var center = new Vector3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z + 0.5f);
        for (int i = 0; i < Faces.Length; i++)
        {
            var face = Faces[i];
            if (isSolid(cell + face)) continue;

            var normal = new Vector3(face.X, face.Y, face.Z);
            _samples.Add(new HullSample(center + normal * SampleInset, normal));
        }
    }

    public void Finish(Vector3 center)
    {
        float radiusSquared = 0f;
        for (int i = 0; i < _samples.Count; i++)
            radiusSquared = MathF.Max(radiusSquared, (_samples[i].Point - center).LengthSquared);
        Radius = MathF.Sqrt(radiusSquared) + 0.5f;
    }
}
