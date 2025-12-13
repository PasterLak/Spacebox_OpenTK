using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using System.Runtime.CompilerServices;

namespace Spacebox.Game.Generation;


public class PackedChunk
{
    public Vector3SByte PositionIndex { get; private set; }
    public int Mass { get; private set; }
    public Vector3 SumPosMass { get; private set; }
    public bool IsModified { get; private set; }
    public PackedBlock[] PackedBlocks { get; private set; }

    public PackedChunk(Chunk chunk)
    {
        PositionIndex = chunk.PositionIndex;
        Mass = chunk.Mass;
        SumPosMass = chunk.SumPosMass;
        IsModified = chunk.IsModified;

        PackedBlocks = new PackedBlock[Chunk.Size * Chunk.Size * Chunk.Size];

        CompressBlocks(chunk.Blocks);
    }

    private void CompressBlocks(Block[,,] sourceBlocks)
    {
        int index = 0;
        for (int x = 0; x < Chunk.Size; x++)
        {
            for (int y = 0; y < Chunk.Size; y++)
            {
                for (int z = 0; z < Chunk.Size; z++)
                {
                    PackedBlocks[index++] = new PackedBlock(sourceBlocks[x, y, z]);
                }
            }
        }
    }

    public Chunk ToChunk(SpaceEntity spaceEntity)
    {
        var blocks = DecompressBlocks();

        var chunk = new Chunk(
            PositionIndex,
            spaceEntity,
            blocks,
            isLoaded: true,
            emptyChunk: false
        );

        chunk.Mass = Mass;
        chunk.SumPosMass = SumPosMass;
        chunk.IsModified = IsModified;

        if (IsModified)
        {
            chunk.NeedsToRegenerateMesh = true;
        }

        return chunk;
    }

    private Block[,,] DecompressBlocks()
    {
        var blocks = new Block[Chunk.Size, Chunk.Size, Chunk.Size];
        int index = 0;

        for (int x = 0; x < Chunk.Size; x++)
        {
            for (int y = 0; y < Chunk.Size; y++)
            {
                for (int z = 0; z < Chunk.Size; z++)
                {
                    var packed = PackedBlocks[index++];

                    blocks[x, y, z] = packed.ToBlock();
                }
            }
        }

        return blocks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateFromChunk(Chunk chunk)
    {
        PositionIndex = chunk.PositionIndex;
        Mass = chunk.Mass;
        SumPosMass = chunk.SumPosMass;
        IsModified = chunk.IsModified;

        CompressBlocks(chunk.Blocks);
    }
}
