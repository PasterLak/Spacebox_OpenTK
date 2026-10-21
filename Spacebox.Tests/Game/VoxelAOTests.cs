using Engine;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Tests;

public class VoxelAOTests
{
    private const int Bottom = 1 << 0;
    private const int BottomLeft = 1 << 1;
    private const int Left = 1 << 2;
    private const int UpperLeft = 1 << 3;
    private const int Top = 1 << 4;
    private const int Right = 1 << 6;

    private const int LowerLeftVertex = 0;
    private const int LowerRightVertex = 1;
    private const int UpperRightVertex = 2;
    private const int UpperLeftVertex = 3;

    private const short TestBlockId = 4000;
    private const int FloatsPerVertex = BuffersData.FloatsPerVertexBlock;

    [Theory]
    [InlineData(false, false, false, 3)]
    [InlineData(false, false, true, 2)]
    [InlineData(true, false, false, 2)]
    [InlineData(false, true, true, 1)]
    [InlineData(true, true, false, 0)]
    [InlineData(true, true, true, 0)]
    public void VertexLevel_FollowsTwoSidesRule(bool side1, bool side2, bool corner, int expected)
    {
        Assert.Equal(expected, AOShading.VertexLevel(side1, side2, corner));
    }

    [Fact]
    public void Brightness_IsMonotonicAndFullAtMaxLevel()
    {
        Assert.Equal(1f, AOShading.Brightness(AOShading.MaxLevel));
        for (int level = 0; level < AOShading.MaxLevel; level++)
            Assert.True(AOShading.Brightness(level) < AOShading.Brightness(level + 1));
    }

    [Fact]
    public void Mask_Empty_LeavesAllVerticesLit()
    {
        for (int vertex = 0; vertex < 4; vertex++)
            Assert.Equal(1f, AOShading.Get(0, vertex));
    }

    [Fact]
    public void Mask_TwoSidesWithoutCorner_IsFullyOccluded()
    {
        byte mask = Bottom | Left;

        Assert.Equal(AOShading.Brightness(0), AOShading.Get(mask, LowerLeftVertex));
        Assert.Equal(AOShading.Brightness(2), AOShading.Get(mask, LowerRightVertex));
        Assert.Equal(AOShading.Brightness(2), AOShading.Get(mask, UpperLeftVertex));
        Assert.Equal(1f, AOShading.Get(mask, UpperRightVertex));
    }

    [Fact]
    public void Mask_CornerOnly_DarkensOnlyThatVertex()
    {
        byte mask = BottomLeft;

        Assert.Equal(AOShading.Brightness(2), AOShading.Get(mask, LowerLeftVertex));
        Assert.Equal(1f, AOShading.Get(mask, LowerRightVertex));
        Assert.Equal(1f, AOShading.Get(mask, UpperRightVertex));
        Assert.Equal(1f, AOShading.Get(mask, UpperLeftVertex));
    }

    [Fact]
    public void Mask_SideAndCorner_GivesLevelOne()
    {
        byte mask = Left | UpperLeft;

        Assert.Equal(AOShading.Brightness(1), AOShading.Get(mask, UpperLeftVertex));
        Assert.Equal(AOShading.Brightness(2), AOShading.Get(mask, LowerLeftVertex));
    }

    [Fact]
    public void Mask_WallOnOneSide_DarkensBothVerticesOfThatEdge()
    {
        byte mask = Top;

        Assert.Equal(AOShading.Get(mask, UpperLeftVertex), AOShading.Get(mask, UpperRightVertex));
        Assert.Equal(AOShading.Brightness(2), AOShading.Get(mask, UpperRightVertex));
        Assert.Equal(1f, AOShading.Get(mask, LowerLeftVertex));
        Assert.Equal(1f, AOShading.Get(Right, LowerLeftVertex));
    }

    [Theory]
    [InlineData(0.25f, 1f, 1f, 1f, false)]
    [InlineData(1f, 0.25f, 1f, 1f, true)]
    [InlineData(1f, 1f, 0.25f, 1f, false)]
    [InlineData(1f, 1f, 1f, 0.25f, true)]
    [InlineData(1f, 1f, 1f, 1f, false)]
    public void ShouldFlip_PutsDiagonalThroughDarkerPair(float lowerLeft, float lowerRight, float upperRight, float upperLeft, bool expected)
    {
        Assert.Equal(expected, AOShading.ShouldFlip(lowerLeft, lowerRight, upperRight, upperLeft));
    }

    [Fact]
    public void Mesh_InnerCorner_DarkensOnlyTheCornerVertex()
    {
        var mesh = Generate(Solid(), (1, 1, 1), (0, 2, 1), (1, 2, 0));
        var top = TopFace(mesh, 1, 1, 1);

        Assert.Equal(AOShading.Brightness(0), top[(1, 1)].Ao, 2);
        Assert.Equal(AOShading.Brightness(2), top[(2, 1)].Ao, 2);
        Assert.Equal(AOShading.Brightness(2), top[(1, 2)].Ao, 2);
        Assert.Equal(1f, top[(2, 2)].Ao);
    }

    [Fact]
    public void Mesh_DiagonalBlock_DarkensOnlyTheNearestVertex()
    {
        var mesh = Generate(Solid(), (1, 1, 1), (2, 2, 2));
        var top = TopFace(mesh, 1, 1, 1);

        Assert.Equal(AOShading.Brightness(2), top[(2, 2)].Ao, 2);
        Assert.Equal(1f, top[(1, 1)].Ao);
        Assert.Equal(1f, top[(2, 1)].Ao);
        Assert.Equal(1f, top[(1, 2)].Ao);
    }

    [Fact]
    public void Mesh_NoNeighbours_HasNoOcclusion()
    {
        var mesh = Generate(Solid(), (1, 1, 1));
        var top = TopFace(mesh, 1, 1, 1);

        Assert.All(top.Values, v => Assert.Equal(1f, v.Ao));
    }

    [Fact]
    public void Mesh_InnerCorner_SplitsQuadThroughDarkVertex()
    {
        var mesh = Generate(Solid(), (1, 1, 1), (0, 2, 1), (1, 2, 0));
        var top = TopFace(mesh, 1, 1, 1);

        var diagonal = SharedDiagonal(mesh, top.Values.Select(v => v.Index).ToArray());

        Assert.Contains(top[(1, 1)].Index, diagonal);
        Assert.Contains(top[(2, 2)].Index, diagonal);
    }

    [Fact]
    public void Mesh_OpposingCorner_SplitsQuadThroughDarkVertex()
    {
        var mesh = Generate(Solid(), (1, 1, 1), (2, 2, 1), (1, 2, 2));
        var top = TopFace(mesh, 1, 1, 1);

        var diagonal = SharedDiagonal(mesh, top.Values.Select(v => v.Index).ToArray());

        Assert.Equal(AOShading.Brightness(0), top[(2, 2)].Ao, 2);
        Assert.Contains(top[(2, 2)].Index, diagonal);
        Assert.Contains(top[(1, 1)].Index, diagonal);
    }

    [Theory]
    [InlineData(Direction.Forward, Rotation.None)]
    [InlineData(Direction.Left, Rotation.Right)]
    [InlineData(Direction.Down, Rotation.Half)]
    [InlineData(Direction.Back, Rotation.Left)]
    public void Mesh_RotatedBlock_HasSameOcclusionAsUnrotated(Direction direction, Rotation rotation)
    {
        var rotated = Solid();
        rotated.Direction = direction;
        rotated.Rotation = rotation;

        var expected = TopFace(Generate(Solid(), (1, 1, 1), (0, 2, 1), (2, 2, 2)), 1, 1, 1);
        var actual = TopFace(Generate(rotated, (1, 1, 1), (0, 2, 1), (2, 2, 2)), 1, 1, 1);

        foreach (var corner in expected.Keys)
            Assert.Equal(expected[corner].Ao, actual[corner].Ao, 2);
    }

    [Theory]
    [InlineData(5, 5, 5, 0)]
    [InlineData(0, 5, 5, 1)]
    [InlineData(31, 0, 5, 3)]
    [InlineData(0, 31, 0, 7)]
    public void ChunkBorder_ReportsFaceEdgeAndCornerNeighbours(byte x, byte y, byte z, int expectedCount)
    {
        Span<Vector3SByte> touched = stackalloc Vector3SByte[ChunkBorder.MaxTouchedNeighbors];
        int count = ChunkBorder.GetTouchedNeighbors(new Vector3Byte(x, y, z), Chunk.Size, touched);

        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public void ChunkBorder_CornerCellIncludesDiagonalChunk()
    {
        Span<Vector3SByte> touched = stackalloc Vector3SByte[ChunkBorder.MaxTouchedNeighbors];
        int count = ChunkBorder.GetTouchedNeighbors(new Vector3Byte(31, 0, 31), Chunk.Size, touched);
        var offsets = touched.Slice(0, count).ToArray();

        Assert.Contains(new Vector3SByte(1, -1, 1), offsets);
        Assert.Contains(new Vector3SByte(1, -1, 0), offsets);
        Assert.Contains(new Vector3SByte(0, 0, 1), offsets);
        Assert.DoesNotContain(new Vector3SByte(-1, 0, 0), offsets);
    }

    private static Block Solid() => new Block(TestBlockId, Direction.Up, 1, 1, false, false, false, false);

    private static MeshData Generate(Block center, (int x, int y, int z) centerCell, params (int x, int y, int z)[] occluders)
    {
        var padded = new Block[Chunk.Size + 2, Chunk.Size + 2, Chunk.Size + 2];
        padded[centerCell.x + 1, centerCell.y + 1, centerCell.z + 1] = center;
        foreach (var (x, y, z) in occluders)
            padded[x + 1, y + 1, z + 1] = Solid();

        return new MeshGenerator(Vector3SByte.Zero, padded, false).GenerateMeshData();
    }

    private static Dictionary<(int x, int z), (int Index, float Ao)> TopFace(MeshData mesh, int bx, int by, int bz)
    {
        var result = new Dictionary<(int x, int z), (int Index, float Ao)>();
        int vertexCount = mesh.VertexCount / FloatsPerVertex;

        for (int i = 0; i < vertexCount; i++)
        {
            int o = i * FloatsPerVertex;
            var position = new Vector3(mesh.Vertices[o], mesh.Vertices[o + 1], mesh.Vertices[o + 2]);
            var packed = BlockVertex.Unpack((uint)BitConverter.SingleToInt32Bits(mesh.Vertices[o + 6]));
            var normal = new Vector3(packed.X, packed.Y, packed.Z) * 2f - Vector3.One;
            if (normal.Y < 0.99f || MathF.Abs(position.Y - (by + 1)) > 1e-4f) continue;

            int x = (int)MathF.Round(position.X);
            int z = (int)MathF.Round(position.Z);
            if (x < bx || x > bx + 1 || z < bz || z > bz + 1) continue;

            result[(x, z)] = (i, packed.W);
        }

        Assert.Equal(4, result.Count);
        return result;
    }

    private static int[] SharedDiagonal(MeshData mesh, int[] quad)
    {
        var counts = new Dictionary<int, int>();
        for (int t = 0; t < mesh.IndexCount; t += 3)
        {
            var triangle = new[] { (int)mesh.Indices[t], (int)mesh.Indices[t + 1], (int)mesh.Indices[t + 2] };
            if (!triangle.All(quad.Contains)) continue;
            foreach (var index in triangle)
                counts[index] = counts.GetValueOrDefault(index) + 1;
        }

        return counts.Where(pair => pair.Value == 2).Select(pair => pair.Key).ToArray();
    }
}
