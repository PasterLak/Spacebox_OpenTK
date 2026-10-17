using OpenTK.Mathematics;
using Engine.Utils;
using Engine;
using System.Collections.Generic;
using System;
using Spacebox.Game.Generation;

public struct WormParameters
{
    public byte WormCount;
    public byte MinCount;
    public byte MaxCount;
    public byte WormDiameter;
    public float Deviation;
    public byte MaxDistance;
    public float StepSize;
    public int Seed;

    public WormParameters(byte minCount, byte maxCount, byte wormDiameter, float deviation, byte maxDistance, float stepSize, int seed)
    {
        MinCount = minCount;
        MaxCount = maxCount;
        WormDiameter = wormDiameter;
        Deviation = deviation;
        MaxDistance = maxDistance;
        StepSize = stepSize;
        Seed = seed;
    }
}

public class PerlinWorms
{
    public static void CarvePaddedChunk(int[,,] paddedData, Vector3SByte chunkIdx, int worldSeed, WormParameters p)
    {
        int searchRadius = (int)MathF.Ceiling(p.MaxDistance / (float)Chunk.Size);
        var sphereOffsets = Offsets(p.WormDiameter);
        const float scale = 0.1f;

        float minGlobalX = chunkIdx.X * Chunk.Size - 1 - p.WormDiameter;
        float maxGlobalX = chunkIdx.X * Chunk.Size + Chunk.Size + 1 + p.WormDiameter;
        float minGlobalY = chunkIdx.Y * Chunk.Size - 1 - p.WormDiameter;
        float maxGlobalY = chunkIdx.Y * Chunk.Size + Chunk.Size + 1 + p.WormDiameter;
        float minGlobalZ = chunkIdx.Z * Chunk.Size - 1 - p.WormDiameter;
        float maxGlobalZ = chunkIdx.Z * Chunk.Size + Chunk.Size + 1 + p.WormDiameter;

        for (int cx = -searchRadius; cx <= searchRadius; cx++)
        {
            for (int cy = -searchRadius; cy <= searchRadius; cy++)
            {
                for (int cz = -searchRadius; cz <= searchRadius; cz++)
                {
                    Vector3SByte originChunk = new Vector3SByte(
                        (sbyte)(chunkIdx.X + cx),
                        (sbyte)(chunkIdx.Y + cy),
                        (sbyte)(chunkIdx.Z + cz));

                    int nodeSeed = Hash(originChunk.X, originChunk.Y, originChunk.Z, worldSeed);
                    var rng = new Random(nodeSeed);
                    var noise = new FastNoiseLite(nodeSeed);
                    noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

                    int wormCount = rng.Next(p.MinCount, p.MaxCount + 1);

                    for (int w = 0; w < wormCount; ++w)
                    {
                        Vector3 pos = new Vector3(
                            originChunk.X * Chunk.Size + rng.NextSingle() * Chunk.Size,
                            originChunk.Y * Chunk.Size + rng.NextSingle() * Chunk.Size,
                            originChunk.Z * Chunk.Size + rng.NextSingle() * Chunk.Size);

                        Vector3 dir = RandomUnit(rng);
                        float travelled = 0;
                        float maxSteps = p.MaxDistance / p.StepSize;

                        while (travelled < maxSteps)
                        {
                            if (pos.X >= minGlobalX && pos.X <= maxGlobalX &&
                                pos.Y >= minGlobalY && pos.Y <= maxGlobalY &&
                                pos.Z >= minGlobalZ && pos.Z <= maxGlobalZ)
                            {
                                int bx = (int)MathF.Round(pos.X);
                                int by = (int)MathF.Round(pos.Y);
                                int bz = (int)MathF.Round(pos.Z);

                                foreach (var off in sphereOffsets)
                                {
                                    int lx = (bx + off.X) - (chunkIdx.X * Chunk.Size) + 1;
                                    int ly = (by + off.Y) - (chunkIdx.Y * Chunk.Size) + 1;
                                    int lz = (bz + off.Z) - (chunkIdx.Z * Chunk.Size) + 1;

                                    if (lx >= 0 && lx < 34 && ly >= 0 && ly < 34 && lz >= 0 && lz < 34)
                                    {
                                        paddedData[lx, ly, lz] = 0;
                                    }
                                }
                            }

                            var n = new Vector3(
                                noise.GetNoise(pos.Y * scale, pos.Z * scale),
                                noise.GetNoise(pos.X * scale, pos.Z * scale),
                                noise.GetNoise(pos.X * scale, pos.Y * scale)) - new Vector3(0.5f);
                            dir = Vector3.Normalize(dir + n * p.Deviation);
                            pos += dir * p.StepSize;
                            travelled += p.StepSize;
                        }
                    }
                }
            }
        }
    }

    private static int Hash(int x, int y, int z, int seed)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + x;
            hash = hash * 31 + y;
            hash = hash * 31 + z;
            hash = hash * 31 + seed;
            return hash;
        }
    }

    private static Vector3i[] Offsets(int dia)
    {
        float rad = dia * .5f;
        int r = (int)MathF.Ceiling(rad);
        float r2 = rad * rad;
        var list = new List<Vector3i>(r * r * r);
        for (int x = -r; x <= r; ++x)
            for (int y = -r; y <= r; ++y)
                for (int z = -r; z <= r; ++z)
                    if (x * x + y * y + z * z <= r2)
                        list.Add(new Vector3i(x, y, z));
        return list.ToArray();
    }

    private static Vector3 RandomUnit(Random rng)
    {
        double u = rng.NextDouble();
        double v = rng.NextDouble();
        double θ = 2 * Math.PI * u;
        double φ = Math.Acos(2 * v - 1);
        return new((float)(Math.Sin(φ) * Math.Cos(θ)),
                    (float)(Math.Sin(φ) * Math.Sin(θ)),
                    (float)Math.Cos(φ));
    }
}