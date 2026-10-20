using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Tests.Generation;

public class LightPropagationTests
{
    private const int Last = Chunk.Size - 1;
    private const int Middle = Chunk.Size / 2;

    private static Block Air() => new Block(0, Direction.Up, 0, 0, false, true, false, false);

    private static Block Lamp() => new Block(5, Direction.Up, 1, 1, false, false, true, true)
    {
        LightLevel = 15f,
        LightColor = new Color3Byte(255)
    };

    private static Chunk[] CreateRow(int count)
    {
        var entity = new SpaceEntity(7, Vector3.Zero, null!);
        var chunks = new Chunk[count];

        for (int i = 0; i < count; i++)
        {
            var chunk = new Chunk(new Vector3SByte((sbyte)i, (sbyte)0, (sbyte)0), entity, emptyChunk: true);
            for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
            for (int z = 0; z < Chunk.Size; z++)
                chunk.Blocks[x, y, z] = Air();
            chunks[i] = chunk;
        }

        for (int i = 1; i < count; i++)
        {
            chunks[i].AddNeighbor(chunks[i - 1]);
            chunks[i - 1].AddNeighbor(chunks[i]);
        }

        return chunks;
    }

    [Fact]
    public void RelightingFarChunk_KeepsLightFromSourceInOtherChunk()
    {
        var chunks = CreateRow(3);
        chunks[0].Blocks[Last, Middle, Middle] = Lamp();
        chunks[0].CalculateLighting();

        var lit = chunks[1].Blocks[15, Middle, Middle];
        var before = lit.LightColor;
        Assert.True(lit.LightLevel > 0f);

        chunks[2].CalculateLighting();

        Assert.Equal(before, chunks[1].Blocks[15, Middle, Middle].LightColor);
        Assert.True(chunks[1].Blocks[15, Middle, Middle].LightLevel > 0f);
    }

    [Fact]
    public void RemovingSource_ClearsLightInNeighborChunk()
    {
        var chunks = CreateRow(3);
        chunks[0].Blocks[Last, Middle, Middle] = Lamp();
        chunks[0].CalculateLighting();

        chunks[0].Blocks[Last, Middle, Middle] = Air();
        chunks[0].CalculateLighting();

        Assert.Equal(0f, chunks[1].Blocks[15, Middle, Middle].LightLevel);
        Assert.Equal(0f, chunks[0].Blocks[Last - 1, Middle, Middle].LightLevel);
    }

    [Fact]
    public void RelightingChunk_KeepsLightFromSourceBeyondReach()
    {
        var chunks = CreateRow(2);
        chunks[0].Blocks[10, Middle, Middle] = Lamp();
        chunks[0].CalculateLighting();

        var before = chunks[0].Blocks[24, Middle, Middle].LightColor;
        Assert.NotEqual(Color3Byte.Zero, before);

        chunks[1].CalculateLighting();

        Assert.Equal(before, chunks[0].Blocks[24, Middle, Middle].LightColor);
    }
}
