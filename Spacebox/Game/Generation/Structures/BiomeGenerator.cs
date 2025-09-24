
using Engine.Utils;
using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Structures;


public class BiomeGenerator
{
    private FastNoiseLite noise;
    private WorldGenerator generator;

    public BiomeGenerator(int seed, WorldGenerator generator)
    {
        noise = new FastNoiseLite(seed);
        this.generator = generator;
    }

    private static byte FloatToByte(float value)
    {
        float clamped = Math.Clamp(value, -1f, 1f);
        return (byte)((clamped + 1f) * 127.5f);
    }


    public void GenerateMap(ref Biome[,,] map, ref Vector3i sectorIndex)
    {
        noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        noise.SetFractalType(FastNoiseLite.FractalType.None);
        noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.EuclideanSq);
        noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.CellValue);
        noise.SetFrequency(generator.BiomesMapNoiseFrequency);
       

        int sectorSize = Sector.SizeBlocks;
        Vector3 sectorOffset = new Vector3(
            sectorIndex.X * sectorSize,
            sectorIndex.Y * sectorSize,
            sectorIndex.Z * sectorSize
        );

        for (int x = 0; x < BiomesMap.Resolution; x++)
        {
            for (int y = 0; y < BiomesMap.Resolution; y++)
            {
                for (int z = 0; z < BiomesMap.Resolution; z++)
                {
                    float worldX = sectorOffset.X + (x * BiomesMap.SmallestPoint);
                    float worldY = sectorOffset.Y + (y * BiomesMap.SmallestPoint);
                    float worldZ = sectorOffset.Z + (z * BiomesMap.SmallestPoint);

                    var n = FloatToByte(noise.GetNoise(worldX, worldY, worldZ));
                    map[x, y, z] = World.Generator.GetBiomeByRandomValue(n);
                }
            }
        }
    }

}
