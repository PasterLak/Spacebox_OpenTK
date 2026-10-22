using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Game.Generation.Shapes.Asteroids;

public enum AsteroidShapeKind
{
    Blob,
    Double,
    Torus
}

public sealed class AsteroidShape
{
    private const float Outer = 0.97f;
    private const float WarpFrequency = 1.6f;
    private const float UnionSmoothing = 0.12f;
    private const float NeckSmoothing = 0.2f;
    private const float BiteSmoothing = 0.06f;
    private const float CraterSmoothing = 0.04f;

    private enum PartKind { Ellipsoid, Capsule, Torus }
    private enum CutKind { Sphere, Cone }

    private readonly record struct Part(PartKind Kind, Vector3 A, Vector3 B, float Radius, float Smoothing);
    private readonly record struct Cut(CutKind Kind, Vector3 Center, Vector3 Axis, float Size, float Smoothing);

    private readonly List<Part> _parts = new List<Part>();
    private readonly List<Cut> _cuts = new List<Cut>();
    private readonly Quaternion _inverseRotation;
    private readonly FastNoiseLite _warpNoise;
    private readonly float _warp;
    private readonly float _maxInside;

    public AsteroidShapeKind Kind { get; }
    public float Roughness { get; }
    public bool RemoveFloatingBlocks { get; }
    public bool HasBite { get; private set; }
    public int CraterCount { get; private set; }
    public int LobeCount { get; private set; }

    public AsteroidShape(AsteroidShapeJSON settings, int seed, AsteroidShapeKind? forcedKind = null)
    {
        settings ??= new AsteroidShapeJSON();
        var random = new Random(seed ^ 0x3C6EF372);

        _warp = Math.Clamp(settings.Warp, 0f, 0.6f);
        Roughness = Math.Clamp(settings.Roughness, 0f, 1f);
        RemoveFloatingBlocks = settings.RemoveFloatingBlocks;
        _inverseRotation = Quaternion.Invert(RandomRotation(random));
        _warpNoise = new FastNoiseLite(seed ^ 0x1B873593);
        _warpNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _warpNoise.SetFrequency(1f);

        float fit = 1f - 0.35f * _warp - 0.25f * Roughness;
        Kind = forcedKind ?? RollKind(settings, random);

        switch (Kind)
        {
            case AsteroidShapeKind.Torus:
                BuildTorus(settings.Torus, random, fit);
                break;
            case AsteroidShapeKind.Double:
                BuildDouble(settings.Double, settings.Stretch, random, fit);
                break;
            default:
                BuildBlob(settings, random, fit);
                break;
        }

        AddBite(settings.Bite, random);
        AddCraters(settings.Craters, random);
        _maxInside = MeasureMaxInside();
    }

    public float Distance(Vector3 local, float radius)
    {
        var q = local / radius;
        var p = Vector3.Transform(q, _inverseRotation);
        if (_warp > 0f)
        {
            var s = p * WarpFrequency;
            p += _warp * new Vector3(
                _warpNoise.GetNoise(s.X, s.Y, s.Z),
                _warpNoise.GetNoise(s.X + 31.7f, s.Y - 12.3f, s.Z + 5.1f),
                _warpNoise.GetNoise(s.X - 8.9f, s.Y + 44.2f, s.Z - 27.5f));
        }

        return MathF.Max(Evaluate(p), q.Length - Outer);
    }

    public float Depth(Vector3 local, float radius)
    {
        return DepthFromDistance(Distance(local, radius));
    }

    public float DepthFromDistance(float distance)
    {
        return Math.Clamp(-distance / _maxInside, 0f, 1f);
    }

    private static AsteroidShapeKind RollKind(AsteroidShapeJSON settings, Random random)
    {
        if (random.Next(100) < settings.Torus.Chance) return AsteroidShapeKind.Torus;
        if (random.Next(100) < settings.Double.Chance) return AsteroidShapeKind.Double;
        return AsteroidShapeKind.Blob;
    }

    private void BuildBlob(AsteroidShapeJSON settings, Random random, float fit)
    {
        float stretch = Math.Clamp(settings.Stretch, 0f, 0.8f);
        var radii = new Vector3(Axis(random, stretch), Axis(random, stretch), Axis(random, stretch));
        float largest = MathF.Max(radii.X, MathF.Max(radii.Y, radii.Z));
        radii *= 0.78f * fit / largest;
        _parts.Add(new Part(PartKind.Ellipsoid, Vector3.Zero, radii, 0f, 0f));

        var lobes = settings.Lobes;
        if (random.Next(100) >= lobes.Chance) return;

        LobeCount = random.Next(lobes.MinCount, lobes.MaxCount + 1);
        for (int i = 0; i < LobeCount; i++)
        {
            float size = Lerp(random, lobes.MinSize, lobes.MaxSize) * 0.6f * fit;
            var center = RandomDirection(random) * (0.88f * fit - size);
            var lobeRadii = new Vector3(Axis(random, stretch), Axis(random, stretch), Axis(random, stretch)) * size;
            _parts.Add(new Part(PartKind.Ellipsoid, center, lobeRadii, 0f, UnionSmoothing));
        }
    }

    private void BuildDouble(AsteroidDoubleJSON settings, float stretch, Random random, float fit)
    {
        stretch = Math.Clamp(stretch, 0f, 0.8f);
        float first = Lerp(random, settings.MinSize, settings.MaxSize) * 0.62f * fit;
        float second = Lerp(random, settings.MinSize, settings.MaxSize) * 0.62f * fit;
        var direction = RandomDirection(random);
        var a = -direction * (0.92f * fit - first);
        var b = direction * (0.92f * fit - second);

        _parts.Add(new Part(PartKind.Ellipsoid, a, new Vector3(Axis(random, stretch), Axis(random, stretch), Axis(random, stretch)) * first, 0f, 0f));
        _parts.Add(new Part(PartKind.Ellipsoid, b, new Vector3(Axis(random, stretch), Axis(random, stretch), Axis(random, stretch)) * second, 0f, UnionSmoothing));
        float neck = Math.Clamp(settings.Neck, 0.2f, 1f) * MathF.Min(first, second);
        _parts.Add(new Part(PartKind.Capsule, a, b, neck, NeckSmoothing));
    }

    private void BuildTorus(AsteroidTorusJSON settings, Random random, float fit)
    {
        float major = 0.6f * fit;
        float tube = MathF.Min(Lerp(random, settings.MinTube, settings.MaxTube), 0.92f * fit - major);
        _parts.Add(new Part(PartKind.Torus, Vector3.Zero, new Vector3(tube), major, 0f));
    }

    private void AddBite(AsteroidBiteJSON settings, Random random)
    {
        if (random.Next(100) >= settings.Chance) return;

        HasBite = true;
        float size = Lerp(random, settings.MinSize, settings.MaxSize);
        var direction = RandomDirection(random);
        if (random.Next(2) == 0)
        {
            _cuts.Add(new Cut(CutKind.Cone, -direction * 0.12f, direction, 0.45f + size * 0.8f, BiteSmoothing));
            return;
        }

        float surface = SurfaceAlong(direction);
        _cuts.Add(new Cut(CutKind.Sphere, direction * (surface + size * 0.15f), direction, size * 1.25f, BiteSmoothing));
    }

    private void AddCraters(AsteroidCratersJSON settings, Random random)
    {
        CraterCount = random.Next(settings.MinCount, settings.MaxCount + 1);
        for (int i = 0; i < CraterCount; i++)
        {
            float size = Lerp(random, settings.MinSize, settings.MaxSize);
            var direction = RandomDirection(random);
            float surface = SurfaceAlong(direction);
            _cuts.Add(new Cut(CutKind.Sphere, direction * (surface + size * 0.55f), direction, size, CraterSmoothing));
        }
    }

    private float Evaluate(Vector3 p)
    {
        float d = float.MaxValue;
        for (int i = 0; i < _parts.Count; i++)
        {
            var part = _parts[i];
            float value = part.Kind switch
            {
                PartKind.Ellipsoid => ShapeSdf.Ellipsoid(p, part.A, part.B),
                PartKind.Capsule => ShapeSdf.Capsule(p, part.A, part.B, part.Radius),
                _ => ShapeSdf.Torus(p, part.Radius, part.B.X)
            };
            d = i == 0 ? value : ShapeSdf.SmoothUnion(d, value, part.Smoothing);
        }

        for (int i = 0; i < _cuts.Count; i++)
        {
            var cut = _cuts[i];
            float cutter = cut.Kind == CutKind.Cone ? ShapeSdf.Cone(p - cut.Center, cut.Axis, cut.Size) : ShapeSdf.Sphere(p, cut.Center, cut.Size);
            d = ShapeSdf.SmoothSubtract(d, cutter, cut.Smoothing);
        }

        return d;
    }

    private float SurfaceAlong(Vector3 direction)
    {
        float inside = 0f;
        float outside = 1f;
        if (Evaluate(Vector3.Zero) > 0f) return 0.5f;

        for (int i = 0; i < 16; i++)
        {
            float middle = (inside + outside) * 0.5f;
            if (Evaluate(direction * middle) <= 0f) inside = middle;
            else outside = middle;
        }
        return inside;
    }

    private float MeasureMaxInside()
    {
        const int steps = 16;
        float min = 0f;
        for (int x = 0; x < steps; x++)
        for (int y = 0; y < steps; y++)
        for (int z = 0; z < steps; z++)
        {
            var p = new Vector3(x, y, z) / (steps - 1) * 2f - Vector3.One;
            min = MathF.Min(min, Evaluate(p));
        }
        return MathF.Max(0.05f, -min);
    }

    private static float Axis(Random random, float stretch) => 1f + stretch * ((float)random.NextDouble() * 2f - 1f);

    private static float Lerp(Random random, float min, float max)
    {
        if (max < min) (min, max) = (max, min);
        return min + (float)random.NextDouble() * (max - min);
    }

    private static Vector3 RandomDirection(Random random)
    {
        float z = (float)random.NextDouble() * 2f - 1f;
        float angle = (float)random.NextDouble() * MathF.Tau;
        float r = MathF.Sqrt(MathF.Max(0f, 1f - z * z));
        return new Vector3(r * MathF.Cos(angle), r * MathF.Sin(angle), z);
    }

    private static Quaternion RandomRotation(Random random)
    {
        float u1 = (float)random.NextDouble();
        float u2 = (float)random.NextDouble() * MathF.Tau;
        float u3 = (float)random.NextDouble() * MathF.Tau;
        float a = MathF.Sqrt(1f - u1);
        float b = MathF.Sqrt(u1);
        return new Quaternion(a * MathF.Sin(u2), a * MathF.Cos(u2), b * MathF.Sin(u3), b * MathF.Cos(u3));
    }
}
