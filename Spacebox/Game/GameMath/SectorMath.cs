

using Engine.Generation;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.GameMath;

public class SectorMath
{

    public Vector3 GetCenter(Sector sector)
    {
        var SizeBlocksHalf = Sector.SizeBlocksHalf;
        return sector.PositionWorld + new Vector3(SizeBlocksHalf, SizeBlocksHalf, SizeBlocksHalf);
    }

    public Vector3i GetSectorIndex(Vector3 worldPosition)
    {
        int x = (int)Math.Floor(worldPosition.X / Sector.SizeBlocks);
        int y = (int)Math.Floor(worldPosition.Y / Sector.SizeBlocks);
        int z = (int)Math.Floor(worldPosition.Z / Sector.SizeBlocks);
        return new Vector3i(x, y, z);
    }

    public Vector3 GetSectorPosition(Vector3i index)
    {
        return new Vector3(
            index.X * Sector.SizeBlocks,
            index.Y * Sector.SizeBlocks,
            index.Z * Sector.SizeBlocks
        );
    }

    public Vector3[] GenerateAsteroidPositions(ulong Seed, Vector3 sectorWorldPos, int minCount, int maxCount, int rejectionSamples, int radius, bool round)
    {
        var newSeed = SeedHelper.ToIntSeed(Seed);
        Random random = new Random(newSeed);
        var settings = new GeneratorSettings();
        settings.RejectionSamples = rejectionSamples;
        settings.Seed = newSeed;
        settings.Count = random.Next(minCount, maxCount);
        settings.Round = round;

        return SectorPointProvider.CreatePoints(new SimplePoissonDiscGenerator(radius), ref settings, sectorWorldPos).ToArray();
    }

    public Vector3 GetRandomPointOnSphere(Vector3 center, Random _random, float radius)
    {
        double theta = _random.NextDouble() * Math.PI * 2;
        double phi = Math.Acos(2 * _random.NextDouble() - 1);
        float x = center.X + (float)(radius * Math.Sin(phi) * Math.Cos(theta));
        float y = center.Y + (float)(radius * Math.Sin(phi) * Math.Sin(theta));
        float z = center.Z + (float)(radius * Math.Cos(phi));
        return new Vector3(x, y, z);
    }

    public Vector3 GetRandomPositionInside(Sector sector, float margin01, Random _random)
    {
        float margin = Sector.SizeBlocks * margin01;

        var positionWorld = sector.PositionWorld;

        float minX = positionWorld.X + margin;
        float maxX = positionWorld.X + Sector.SizeBlocks - margin;

        float minY = positionWorld.Y + margin;
        float maxY = positionWorld.Y + Sector.SizeBlocks - margin;

        float minZ = positionWorld.Z + margin;
        float maxZ = positionWorld.Z + Sector.SizeBlocks - margin;

        float x = (float)(_random.NextDouble() * (maxX - minX) + minX);
        float y = (float)(_random.NextDouble() * (maxY - minY) + minY);
        float z = (float)(_random.NextDouble() * (maxZ - minZ) + minZ);

        return new Vector3(x, y, z);
    }

    public string IndexToFolderName(Vector3i index)
    {
        int x = index.X;
        int y = index.Y;
        int z = index.Z;

        string xStr = x >= 0 ? "+" + x : "-" + x;
        string yStr = y >= 0 ? "+" + y : "-" + y;
        string zStr = z >= 0 ? "+" + z : "-" + z;

        return "Sector" + xStr + yStr + zStr;
    }
}
