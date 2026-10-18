using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Tests;

public class BlockProximityTests
{
    private sealed class FakeEntity
    {
        private readonly Dictionary<Vector3SByte, Block[,,]> _chunks = new Dictionary<Vector3SByte, Block[,,]>();

        public int Lookups { get; private set; }

        public void Set(Vector3i cell, bool air = false)
        {
            var index = new Vector3SByte((sbyte)FloorDiv(cell.X), (sbyte)FloorDiv(cell.Y), (sbyte)FloorDiv(cell.Z));
            if (!_chunks.TryGetValue(index, out var blocks))
            {
                blocks = new Block[Chunk.Size, Chunk.Size, Chunk.Size];
                _chunks[index] = blocks;
            }

            var block = new Block();
            if (air) block.SetAir(true);
            blocks[cell.X - index.X * Chunk.Size, cell.Y - index.Y * Chunk.Size, cell.Z - index.Z * Chunk.Size] = block;
        }

        public Block[,,]? Lookup(Vector3SByte index)
        {
            Lookups++;
            return _chunks.TryGetValue(index, out var blocks) ? blocks : null;
        }

        private static int FloorDiv(int value) => (int)MathF.Floor(value / (float)Chunk.Size);
    }

    [Fact]
    public void FindsBlockInsideRange()
    {
        var entity = new FakeEntity();
        entity.Set(new Vector3i(5, 5, 5));

        Assert.True(BlockProximity.HasBlockWithin(new Vector3i(5, 5, 15), 16, entity.Lookup));
        Assert.True(BlockProximity.HasBlockWithin(new Vector3i(5, 5, 5), 0, entity.Lookup));
    }

    [Fact]
    public void RangeIsSphericalAndInclusive()
    {
        var entity = new FakeEntity();
        entity.Set(Vector3i.Zero);

        Assert.True(BlockProximity.HasBlockWithin(new Vector3i(16, 0, 0), 16, entity.Lookup));
        Assert.False(BlockProximity.HasBlockWithin(new Vector3i(17, 0, 0), 16, entity.Lookup));
        Assert.False(BlockProximity.HasBlockWithin(new Vector3i(12, 12, 0), 16, entity.Lookup));
    }

    [Fact]
    public void IgnoresAirAndEmptyCells()
    {
        var entity = new FakeEntity();
        entity.Set(new Vector3i(1, 1, 1), air: true);

        Assert.False(BlockProximity.HasBlockWithin(new Vector3i(2, 2, 2), 16, entity.Lookup));
    }

    [Fact]
    public void WorksAcrossChunkBordersAndNegativeChunks()
    {
        var entity = new FakeEntity();
        entity.Set(new Vector3i(-1, -40, 31));

        Assert.True(BlockProximity.HasBlockWithin(new Vector3i(3, -36, 35), 16, entity.Lookup));
        Assert.False(BlockProximity.HasBlockWithin(new Vector3i(30, -40, 31), 16, entity.Lookup));
    }

    [Fact]
    public void RadiusDoesNotGrowWithAsteroidSize()
    {
        var entity = new FakeEntity();
        for (int x = 0; x < 64; x++)
        for (int y = 0; y < 64; y++)
        for (int z = 0; z < 64; z++)
            entity.Set(new Vector3i(x, y, z));

        Assert.True(BlockProximity.HasBlockWithin(new Vector3i(63 + 16, 32, 32), 16, entity.Lookup));
        Assert.False(BlockProximity.HasBlockWithin(new Vector3i(63 + 17, 32, 32), 16, entity.Lookup));
    }

    [Fact]
    public void SkipsChunksOutsideRangeWithoutLookup()
    {
        var entity = new FakeEntity();

        BlockProximity.HasBlockWithin(new Vector3i(16, 16, 16), 4, entity.Lookup);

        Assert.Equal(1, entity.Lookups);
    }

    [Fact]
    public void NegativeRange_FindsNothing()
    {
        var entity = new FakeEntity();
        entity.Set(Vector3i.Zero);

        Assert.False(BlockProximity.HasBlockWithin(Vector3i.Zero, -1, entity.Lookup));
    }
}
