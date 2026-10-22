using Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.ShipViewer;

namespace Spacebox.Tests.ShipViewer;

public class ShipLightFloodTests
{
    private const int Size = Chunk.Size;

    [Fact]
    public void Light_SpreadsAcrossChunksAndFades()
    {
        var chunks = TwoAirChunks();
        chunks[new Vector3SByte(0, 0, 0)][Size - 2, 5, 5] = Lamp();

        ShipLightFlood.Propagate(chunks);

        var near = chunks[new Vector3SByte(0, 0, 0)][Size - 1, 5, 5];
        var across = chunks[new Vector3SByte(1, 0, 0)][0, 5, 5];
        var far = chunks[new Vector3SByte(1, 0, 0)][10, 5, 5];
        Assert.Equal(204, near.LightColor.R);
        Assert.Equal(163, across.LightColor.R);
        Assert.True(far.LightColor.R < across.LightColor.R);
        Assert.True(far.LightLevel > 0f);
    }

    [Fact]
    public void SolidBlocks_StopLight()
    {
        var chunks = TwoAirChunks();
        var blocks = chunks[new Vector3SByte(0, 0, 0)];
        blocks[5, 5, 5] = Lamp();
        blocks[6, 5, 5] = Solid();

        ShipLightFlood.Propagate(chunks);

        Assert.Equal(0, blocks[6, 5, 5].LightColor.R);
        Assert.True(blocks[7, 5, 5].LightColor.R < blocks[4, 5, 5].LightColor.R);
    }

    private static Dictionary<Vector3SByte, Block[,,]> TwoAirChunks()
    {
        return new Dictionary<Vector3SByte, Block[,,]>
        {
            [new Vector3SByte(0, 0, 0)] = AirChunk(),
            [new Vector3SByte(1, 0, 0)] = AirChunk()
        };
    }

    private static Block[,,] AirChunk()
    {
        var blocks = new Block[Size, Size, Size];
        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
        for (int z = 0; z < Size; z++)
            blocks[x, y, z] = new Block(0, Block.DefaultDirection, 0, 0, true, true, false, false);
        return blocks;
    }

    private static Block Lamp()
    {
        var lamp = new Block(2, Block.DefaultDirection, 1, 1, false, false, true, true)
        {
            LightColor = new Color3Byte(255, 255, 255),
            LightLevel = 15f
        };
        return lamp;
    }

    private static Block Solid()
    {
        return new Block(3, Block.DefaultDirection, 1, 1, false, false, false, false);
    }
}
