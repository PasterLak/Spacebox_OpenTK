using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Tests;

public class BlockRotationTests
{
    private static readonly Direction[] Directions = Enum.GetValues<Direction>();
    private static readonly Rotation[] Rotations = Enum.GetValues<Rotation>();

    private static void AssertAxisAligned(Vector3 v)
    {
        var rounded = new Vector3(MathF.Round(v.X), MathF.Round(v.Y), MathF.Round(v.Z));
        Assert.InRange(Vector3.Distance(v, rounded), 0f, 0.0001f);
        Assert.Equal(1f, rounded.Length, 4);
    }

    [Fact]
    public void Transform_TurnsBaseFrontToFacing_ForEveryCombination()
    {
        foreach (var baseFront in Directions)
        foreach (var facing in Directions)
        foreach (var rotation in Rotations)
        {
            var matrix = BlockRotationHelper.CalculateTransformMatrix(baseFront, facing, rotation);
            var front = Vector3.TransformVector(BlockRotationHelper.GetDirectionVector(baseFront), matrix);

            Assert.InRange(Vector3.Distance(BlockRotationHelper.GetDirectionVector(facing), front), 0f, 0.0001f);
        }
    }

    [Fact]
    public void Transform_KeepsGridAxesAligned()
    {
        foreach (var baseFront in Directions)
        foreach (var facing in Directions)
        foreach (var rotation in Rotations)
        {
            var matrix = BlockRotationHelper.CalculateTransformMatrix(baseFront, facing, rotation);

            AssertAxisAligned(Vector3.TransformVector(Vector3.UnitX, matrix));
            AssertAxisAligned(Vector3.TransformVector(Vector3.UnitY, matrix));
            AssertAxisAligned(Vector3.TransformVector(Vector3.UnitZ, matrix));
        }
    }

    [Fact]
    public void Rotations_GiveFourDifferentUpVectors()
    {
        var ups = new HashSet<Vector3>();
        foreach (var rotation in Rotations)
        {
            var faces = BlockRotationHelper.GetTransformedFaceVectors(Direction.Forward, Direction.Forward, rotation);
            var up = faces[Direction.Up];
            ups.Add(new Vector3(MathF.Round(up.X), MathF.Round(up.Y), MathF.Round(up.Z)));
            Assert.Equal(0f, Vector3.Dot(up, faces[Direction.Forward]), 4);
        }

        Assert.Equal(4, ups.Count);
    }

    [Fact]
    public void FaceVectors_AreOppositePairs()
    {
        var faces = BlockRotationHelper.GetTransformedFaceVectors(Direction.Forward, Direction.Left, Rotation.Right);

        Assert.Equal(-1f, Vector3.Dot(faces[Direction.Forward], faces[Direction.Back]), 4);
        Assert.Equal(-1f, Vector3.Dot(faces[Direction.Left], faces[Direction.Right]), 4);
        Assert.Equal(-1f, Vector3.Dot(faces[Direction.Up], faces[Direction.Down]), 4);
    }

    [Fact]
    public void MultiblockOffsets_RoundToCells()
    {
        var matrix = BlockRotationHelper.CalculateTransformMatrix(Direction.Forward, Direction.Right, Rotation.None);

        var offset = MultiblockLayout.ToEntityOffset(new Vector3Byte(1, 2, 3), matrix);

        Assert.Equal(new Vector3i(3, 2, -1), offset);
    }

    [Fact]
    public void ModelFaceAxes_AreOrthonormal()
    {
        foreach (var face in Directions)
        {
            MultiblockLayout.GetModelFaceAxes(face, out var normal, out var up, out var right);

            Assert.Equal(0f, Vector3.Dot(normal, up), 5);
            Assert.Equal(0f, Vector3.Dot(normal, right), 5);
            Assert.Equal(0f, Vector3.Dot(up, right), 5);
            Assert.Equal(BlockRotationHelper.GetDirectionVector(face), normal);
        }
    }

    [Fact]
    public void FaceUVs_SplitTextureAcrossMultiblockCells()
    {
        Vector2[] baseUvs = [new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)];
        var size = new Vector3Byte(2, 2, 1);

        var uvs = MultiblockLayout.GetFaceUVs(baseUvs, new Vector2i(2, 2), size, new Vector3Byte(1, 0, 0), Direction.Forward);
        var back = MultiblockLayout.GetFaceUVs(baseUvs, new Vector2i(2, 2), size, new Vector3Byte(1, 0, 0), Direction.Back);

        Assert.Equal(new Vector2(0.5f, 0f), uvs[0]);
        Assert.Equal(new Vector2(1f, 0.5f), uvs[2]);
        Assert.Equal(new Vector2(0f, 0f), back[0]);
    }

    [Fact]
    public void FaceUVs_WithSingleTile_UsesWholeTexture()
    {
        Vector2[] baseUvs = [new Vector2(0.25f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.75f), new Vector2(0.25f, 0.75f)];

        var uvs = MultiblockLayout.GetFaceUVs(baseUvs, new Vector2i(1, 1), new Vector3Byte(3, 3, 3), new Vector3Byte(2, 1, 0), Direction.Up);

        Assert.Equal(baseUvs, uvs);
    }
}
