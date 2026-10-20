using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Tests.Generation;

public class ChunkLightSourcesTests
{
    private static Block Solid(float lightLevel)
    {
        return new Block(5, Direction.Left, 7, 9, false, false, false, false) { LightLevel = lightLevel };
    }

    [Fact]
    public void OnlyFullLightLevelBlocksAreSources()
    {
        var blocks = new Block[2, 2, 2];
        blocks[0, 0, 0] = Solid(15f);
        blocks[1, 0, 0] = Solid(14f);
        blocks[0, 1, 1] = Solid(15f);

        var cells = new ChunkLightSources().Get(blocks, 1);

        Assert.Equal(new[] { new Vector3i(0, 0, 0), new Vector3i(0, 1, 1) }, cells);
    }

    [Fact]
    public void Cells_AreCachedUntilTheChunkChanges()
    {
        var blocks = new Block[1, 1, 2];
        blocks[0, 0, 0] = Solid(15f);
        var sources = new ChunkLightSources();

        Assert.Single(sources.Get(blocks, 3));

        blocks[0, 0, 1] = Solid(15f);
        Assert.Single(sources.Get(blocks, 3));
        Assert.Equal(2, sources.Get(blocks, 4).Count);
    }
}
