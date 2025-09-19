
using Engine;
using OpenTK.Mathematics;
using static Engine.Input;

namespace Spacebox.Game.Generation.Structures;

public class BiomesMap
{
    public const byte Resolution = 128;
    public const byte SmallestPoint = (byte)(Sector.SizeBlocks/ Resolution);
    private Biome[,,] map;
    private BiomeGenerator generator;

    public BiomesMap(Vector3i sectorIndex, BiomeGenerator generator)
    {
        map = new Biome[Resolution, Resolution, Resolution];
        this.generator = generator;

        PopulateMap(ref map, ref sectorIndex);

        //SaveAsTexture("NOISE.png");
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

    public Biome GetFromSectorLocalCoord(Vector3 sectorLocalPos)
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

    public void SaveAsTexture(string path)
    {
        Texture2D texture = new Texture2D(Resolution, Resolution);

        for (int x = 0; x < Resolution; x++)
        {
            for (int y = 0; y < Resolution; y++)
            {

                texture.SetPixel(x, y, map[x, y, Resolution/2].DebugColor);
                //texture.SetPixel(x, y, Color3Byte.Yellow);
            }
        }

        texture.SaveToPng(path);
    }
}
