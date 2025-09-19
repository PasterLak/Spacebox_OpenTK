
using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Structures;

public class BiomesMap
{
    public const byte Resolution = byte.MaxValue;
    public const byte SmallestPoint = (byte)(Sector.SizeBlocks/ Resolution);
    private Biome[,,] map;
    private BiomeGenerator generator;

    public BiomesMap(Vector3i sectorIndex, BiomeGenerator generator)
    {
        map = new Biome[Resolution, Resolution, Resolution];
        this.generator = generator;

        PopulateMap(ref map, ref sectorIndex);
    }

    private void PopulateMap(ref Biome[,,] map, ref Vector3i sectorIndex)
    {
        generator.GenerateMap(ref map, ref sectorIndex);
    }

    public Biome GetValue(Vector3Byte coord)
    {
        return map[coord.X, coord.Y, coord.Z];
    }
    public Biome GetValue(byte x, byte y , byte z)
    {
        return map[x,y,z];
    }

    public Biome GetValueFromSectorLocalCoord(Vector3 sectorLocalPos)
    {
        byte x = (byte)Math.Clamp((int)(sectorLocalPos.X / SmallestPoint), 0, Resolution - 1);
        byte y = (byte)Math.Clamp((int)(sectorLocalPos.Y / SmallestPoint), 0, Resolution - 1);
        byte z = (byte)Math.Clamp((int)(sectorLocalPos.Z / SmallestPoint), 0, Resolution - 1);
        return map[x, y, z];
    }

    public Biome GetValueFromSectorLocalCoord(Vector3i sectorLocalPos)
    {
        byte x = (byte)Math.Clamp(sectorLocalPos.X / SmallestPoint, 0, Resolution - 1);
        byte y = (byte)Math.Clamp(sectorLocalPos.Y / SmallestPoint, 0, Resolution - 1);
        byte z = (byte)Math.Clamp(sectorLocalPos.Z / SmallestPoint, 0, Resolution - 1);
        return map[x, y, z];
    }

    public Vector3Byte SectorLocalToMapCoord(Vector3 sectorLocalPos)
    {
        byte x = (byte)Math.Clamp((int)(sectorLocalPos.X / SmallestPoint), 0, Resolution - 1);
        byte y = (byte)Math.Clamp((int)(sectorLocalPos.Y / SmallestPoint), 0, Resolution - 1);
        byte z = (byte)Math.Clamp((int)(sectorLocalPos.Z / SmallestPoint), 0, Resolution - 1);
        return new Vector3Byte(x, y, z);
    }

    public Vector3Byte SectorLocalToMapCoord(Vector3i sectorLocalPos)
    {
        byte x = (byte)Math.Clamp(sectorLocalPos.X / SmallestPoint, 0, Resolution - 1);
        byte y = (byte)Math.Clamp(sectorLocalPos.Y / SmallestPoint, 0, Resolution - 1);
        byte z = (byte)Math.Clamp(sectorLocalPos.Z / SmallestPoint, 0, Resolution - 1);
        return new Vector3Byte(x, y, z);
    }
}
