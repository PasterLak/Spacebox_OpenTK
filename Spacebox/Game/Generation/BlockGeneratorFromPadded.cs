using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation;

public class BlockGeneratorFromPadded : BlockGenerator
{
    private readonly Block[,,] _paddedBlocks;

    public BlockGeneratorFromPadded(Chunk chunk, Vector3 position, Block[,,] paddedBlocks) : base(chunk, position)
    {
        _paddedBlocks = paddedBlocks;
    }

    public override void Generate()
    {
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                for (int z = 0; z < Size; z++)
                    _blocks[x, y, z] = _paddedBlocks[x + 1, y + 1, z + 1];
    }
}
