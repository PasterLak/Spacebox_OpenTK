using Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Resource;

namespace Spacebox.Tests;

public class MultiblockRepairTests
{
    private const short Air = 0;
    private const short Stone = 1;
    private const short Tall = 2;

    private static readonly BlockData TallData = new BlockData("tall", "generator")
    {
        Size = new Vector3Byte(1, 2, 1),
        BaseFrontDirection = Direction.Up
    };

    private static readonly BlockData StoneData = new BlockData("stone", "block");

    private static BlockData DataOf(short id) => id == Tall ? TallData : id == Stone ? StoneData : null;

    private static Block Create(short id)
    {
        return new Block(id, Block.DefaultDirection, 1, 1, false, id == Air, false, false);
    }

    private static MultiblockRepair Repair() => new MultiblockRepair(DataOf, Create);

    private static Dictionary<Vector3SByte, Block[,,]> World(params Vector3SByte[] chunks)
    {
        var world = new Dictionary<Vector3SByte, Block[,,]>();
        foreach (var index in chunks)
        {
            var blocks = new Block[Chunk.Size, Chunk.Size, Chunk.Size];
            for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
            for (int z = 0; z < Chunk.Size; z++)
                blocks[x, y, z] = Create(Air);
            world[index] = blocks;
        }
        return world;
    }

    [Fact]
    public void OldSingleCell_IsCompletedWhenTheCellAboveIsFree()
    {
        var world = World(Vector3SByte.Zero);
        var blocks = world[Vector3SByte.Zero];
        blocks[3, 3, 3] = Create(Tall);

        var result = Repair().Repair(world);

        Assert.Equal(1, result.Completed);
        Assert.Equal(Tall, blocks[3, 3, 3].Id);
        Assert.Equal(Tall, blocks[3, 4, 3].Id);
        Assert.Equal(new Vector3Byte(0, 1, 0), blocks[3, 4, 3].MultiblockOffset);
    }

    [Fact]
    public void OldSingleCell_IsRemovedWhenBlocked()
    {
        var world = World(Vector3SByte.Zero);
        var blocks = world[Vector3SByte.Zero];
        blocks[3, 3, 3] = Create(Tall);
        blocks[3, 4, 3] = Create(Stone);

        var result = Repair().Repair(world);

        Assert.Equal(1, result.Removed);
        Assert.True(blocks[3, 3, 3].IsAir);
        Assert.Equal(Stone, blocks[3, 4, 3].Id);
    }

    [Fact]
    public void OldSingleCell_IsRemovedWhenTheStructureLeavesLoadedChunks()
    {
        var world = World(Vector3SByte.Zero);
        var blocks = world[Vector3SByte.Zero];
        blocks[3, Chunk.Size - 1, 3] = Create(Tall);

        var result = Repair().Repair(world);

        Assert.Equal(1, result.Removed);
        Assert.True(blocks[3, Chunk.Size - 1, 3].IsAir);
    }

    [Fact]
    public void StructureAcrossChunks_IsCompleted()
    {
        var above = new Vector3SByte(0, 1, 0);
        var world = World(Vector3SByte.Zero, above);
        world[Vector3SByte.Zero][3, Chunk.Size - 1, 3] = Create(Tall);

        Repair().Repair(world);

        Assert.Equal(Tall, world[above][3, 0, 3].Id);
    }

    [Fact]
    public void CompleteStructure_IsUnchanged()
    {
        var world = World(Vector3SByte.Zero);
        var blocks = world[Vector3SByte.Zero];
        blocks[3, 3, 3] = Create(Tall);
        var part = Create(Tall);
        part.MultiblockOffset = new Vector3Byte(0, 1, 0);
        blocks[3, 4, 3] = part;

        var result = Repair().Repair(world);

        Assert.False(result.Changed);
        Assert.Same(part, blocks[3, 4, 3]);
    }

    [Fact]
    public void PartWithoutOrigin_IsRemoved()
    {
        var world = World(Vector3SByte.Zero);
        var blocks = world[Vector3SByte.Zero];
        var part = Create(Tall);
        part.MultiblockOffset = new Vector3Byte(0, 1, 0);
        blocks[3, 4, 3] = part;

        var result = Repair().Repair(world);

        Assert.Equal(1, result.OrphanPartsRemoved);
        Assert.True(blocks[3, 4, 3].IsAir);
    }

    [Fact]
    public void BlockedStructure_IsReportedWithItsCellAndOriginalBlock()
    {
        var world = World(Vector3SByte.Zero);
        var blocks = world[Vector3SByte.Zero];
        var origin = Create(Tall);
        blocks[3, 3, 3] = origin;
        blocks[3, 4, 3] = Create(Stone);

        var result = Repair().Repair(world);

        var broken = Assert.Single(result.Broken);
        Assert.Equal(new OpenTK.Mathematics.Vector3i(3, 3, 3), broken.Cell);
        Assert.Same(origin, broken.Block);
    }

    [Fact]
    public void StructureLeavingLoadedChunks_IsReportedInItsChunkCell()
    {
        var chunk = new Vector3SByte(1, 0, 0);
        var world = World(chunk);
        var origin = Create(Tall);
        world[chunk][2, Chunk.Size - 1, 5] = origin;

        var result = Repair().Repair(world);

        var broken = Assert.Single(result.Broken);
        Assert.Equal(new OpenTK.Mathematics.Vector3i(Chunk.Size + 2, Chunk.Size - 1, 5), broken.Cell);
        Assert.Same(origin, broken.Block);
    }

    [Fact]
    public void CompletedAndOrphanRepairs_ReportNoBrokenStructures()
    {
        var world = World(Vector3SByte.Zero);
        var blocks = world[Vector3SByte.Zero];
        blocks[3, 3, 3] = Create(Tall);
        var part = Create(Tall);
        part.MultiblockOffset = new Vector3Byte(0, 1, 0);
        blocks[8, 4, 8] = part;

        var result = Repair().Repair(world);

        Assert.True(result.Changed);
        Assert.Null(result.Broken);
    }
}
