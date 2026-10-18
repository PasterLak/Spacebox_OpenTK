using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation;

namespace Spacebox.Tests;

public class GameMathTests
{
    [Theory]
    [InlineData(0f, 0f, 0f, 0, 0, 0)]
    [InlineData(8191.9f, 0f, 8192f, 0, 0, 1)]
    [InlineData(-0.1f, -8192f, -8192.5f, -1, -1, -2)]
    public void GetSectorIndex_FloorsNegativeCoordinates(float x, float y, float z, int ix, int iy, int iz)
    {
        Assert.Equal(new Vector3i(ix, iy, iz), SpaceMath.Sector.GetSectorIndex(new Vector3(x, y, z)));
    }

    [Fact]
    public void GetSectorPosition_IsInverseOfIndex()
    {
        var index = new Vector3i(3, -2, 7);
        var position = SpaceMath.Sector.GetSectorPosition(index);

        Assert.Equal(new Vector3(3 * Sector.SizeBlocks, -2 * Sector.SizeBlocks, 7 * Sector.SizeBlocks), position);
        Assert.Equal(index, SpaceMath.Sector.GetSectorIndex(position + new Vector3(1f)));
    }

    [Theory]
    [InlineData(0, 0, 0, "Sector+0+0+0")]
    [InlineData(-1, 2, -30, "Sector-1+2-30")]
    public void IndexToFolderName_UsesSigns(int x, int y, int z, string expected)
    {
        Assert.Equal(expected, SpaceMath.Sector.IndexToFolderName(new Vector3i(x, y, z)));
    }

    [Theory]
    [InlineData(0f, 0f, 0f, 0, 0, 0)]
    [InlineData(31.9f, 32f, -0.5f, 0, 1, -1)]
    [InlineData(-32f, -33f, 64f, -1, -2, 2)]
    public void GetChunkIndex_IsRelativeToEntity(float x, float y, float z, sbyte ix, sbyte iy, sbyte iz)
    {
        var entityPosition = new Vector3(100f, -50f, 10f);

        var index = SpaceMath.Chunk.GetChunkIndex(entityPosition + new Vector3(x, y, z), entityPosition);

        Assert.Equal(new Vector3SByte(ix, iy, iz), index);
    }

    [Fact]
    public void ChunkIndexToLocal_ScalesByChunkSize()
    {
        Assert.Equal(new Vector3(-Chunk.Size, 0, 2 * Chunk.Size), SpaceMath.Entity.ChunkIndexToLocal(new Vector3SByte(-1, 0, 2)));
    }

    [Fact]
    public void DistanceToBox_IsSquaredDistanceOutsideAndZeroInside()
    {
        var box = BoundingBox.CreateFromMinMax(Vector3.Zero, new Vector3(10));

        Assert.Equal(0f, SpaceMath.World.DistanceToBox(new Vector3(5), box));
        Assert.Equal(9f, SpaceMath.World.DistanceToBox(new Vector3(13, 5, 5), box));
        Assert.Equal(3f, SpaceMath.World.DistanceToBox(new Vector3(-1, -1, -1), box));
    }

    [Fact]
    public void DistanceToEdge_UsesDirection()
    {
        var local = new Vector3(10, 20, 30);

        Assert.Equal(10f, SpaceMath.World.DistanceToEdge(local, 100, new Vector3i(-1, 0, 0)));
        Assert.Equal(80f, SpaceMath.World.DistanceToEdge(local, 100, new Vector3i(0, 1, 0)));
        Assert.Equal(70f, SpaceMath.World.DistanceToEdge(local, 100, new Vector3i(0, 0, 1)));
        Assert.Equal(float.MaxValue, SpaceMath.World.DistanceToEdge(local, 100, Vector3i.Zero));
    }

    [Fact]
    public void IsPositionInside_ChecksWorldBounds()
    {
        float half = World.SizeSectors * (float)Sector.SizeBlocks / 2f;

        Assert.True(SpaceMath.World.IsPositionInside(Vector3.Zero));
        Assert.True(SpaceMath.World.IsPositionInside(new Vector3(half, -half, 0)));
        Assert.False(SpaceMath.World.IsPositionInside(new Vector3(half * 1.01f, 0, 0)));
    }

    [Fact]
    public void GetRandomPointAroundPosition_StaysInRange()
    {
        var center = new Vector3(5, 5, 5);
        for (int i = 0; i < 100; i++)
        {
            float distance = Vector3.Distance(center, SpaceMath.World.GetRandomPointAroundPosition(center, 10f, 2f));
            Assert.InRange(distance, 1.999f, 10.001f);
        }
    }
}
