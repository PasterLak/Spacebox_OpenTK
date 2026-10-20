using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation;

public sealed class ChunkLightSources
{
    public const float SourceLevel = 15f;

    private readonly List<Vector3i> _cells = new List<Vector3i>();
    private int _version = -1;

    public IReadOnlyList<Vector3i> Cells => _cells;

    public static bool IsSource(Block block) => block != null && !block.IsAir && block.LightLevel >= SourceLevel;

    public IReadOnlyList<Vector3i> Get(Block[,,] blocks, int dataVersion)
    {
        if (dataVersion == _version) return _cells;

        _version = dataVersion;
        _cells.Clear();

        int sizeX = blocks.GetLength(0);
        int sizeY = blocks.GetLength(1);
        int sizeZ = blocks.GetLength(2);
        for (int x = 0; x < sizeX; x++)
        for (int y = 0; y < sizeY; y++)
        for (int z = 0; z < sizeZ; z++)
        {
            if (IsSource(blocks[x, y, z])) _cells.Add(new Vector3i(x, y, z));
        }

        return _cells;
    }
}
