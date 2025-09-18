using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Engine.Generation
{
    public struct GeneratorSettings
    {

        public int Count;
        public int RejectionSamples;
        public int Seed;
        public bool Round;
    }

    public interface IPointGenerator
    {
        List<Vector3> Generate(in GeneratorSettings settings, Vector3 sectorIndex);

        public static float RoundFloat(float x)
        {
            return (float)Math.Round(x, MidpointRounding.ToEven);
        }

        public static Vector3 RoundVector3(Vector3 v)
        {
            return new Vector3(RoundFloat(v.X), RoundFloat(v.Y), RoundFloat(v.Z));
        }
    }

    public class FarthestPointGenerator : IPointGenerator
    {
        public List<Vector3> Generate(in GeneratorSettings settings, Vector3 sectorIndex)
        {
            var size = new Vector3(Sector.SizeBlocks);
            var rng = new Random(settings.Seed);
            var points = new List<Vector3>(settings.Count);

            points.Add(sectorIndex + (settings.Round ? IPointGenerator.RoundVector3(RandomPoint(rng, size)) : RandomPoint(rng, size)));
            while (points.Count < settings.Count)
            {
                Vector3 best = default;
                float bestDist = -1f;
                for (int i = 0; i < settings.RejectionSamples; i++)
                {
                    var cand = RandomPoint(rng, size);

                    if (settings.Round)
                    {
                        cand = IPointGenerator.RoundVector3(cand);
                    }

                    float dmin = float.MaxValue;
                    foreach (var p in points)
                    {
                        float d = (p - cand).LengthSquared;
                        if (d < dmin) dmin = d;
                    }
                    if (dmin > bestDist)
                    {
                        bestDist = dmin;
                        best = cand;
                    }
                }
                points.Add(sectorIndex + best);
            }
            return points;
        }

        Vector3 RandomPoint(Random rng, Vector3 size)
        {
            return new Vector3(
                (float)rng.NextDouble() * size.X,
                (float)rng.NextDouble() * size.Y,
                (float)rng.NextDouble() * size.Z
            );
        }
    }

    public class SimplePoissonDiscGenerator : IPointGenerator
    {

        private int radius;
        public SimplePoissonDiscGenerator(int radius)
        {
            this.radius = radius;
        }
        public List<Vector3> Generate(in GeneratorSettings settings, Vector3 sectorIndex)
        {
            var size = new Vector3(Sector.SizeBlocks);
            var rng = new Random(settings.Seed);
            var points = new List<Vector3>();
            float minDist2 = radius * radius;

            points.Add(sectorIndex + (settings.Round ? IPointGenerator.RoundVector3(RandomPoint(rng, size)) : RandomPoint(rng, size)));

            while (points.Count < settings.Count)
            {
                bool placed = false;
                for (int i = 0; i < settings.RejectionSamples; i++)
                {
                    var cand = RandomPoint(rng, size);

                    if (settings.Round)
                    {
                        cand = IPointGenerator.RoundVector3(cand);
                    }
                    bool ok = true;
                    foreach (var p in points)
                    {
                        if ((p - cand).LengthSquared < minDist2)
                        {
                            ok = false;
                            break;
                        }
                    }
                    if (ok)
                    {
                        points.Add(sectorIndex + cand);
                        placed = true;
                        break;
                    }
                }
                if (!placed)
                    break;
            }
            return points;
        }

        private Vector3 RandomPoint(Random rng, Vector3 size)
        {
            return new Vector3(
                (float)rng.NextDouble() * size.X,
                (float)rng.NextDouble() * size.Y,
                (float)rng.NextDouble() * size.Z
            );
        }
    }

    public static class SectorPointProvider
    {
        public static List<Vector3> CreatePoints(IPointGenerator generator, ref GeneratorSettings settings, Vector3 sectorWorldPos
            )
        {
            return generator.Generate(in settings, sectorWorldPos);
        }
    }
}
