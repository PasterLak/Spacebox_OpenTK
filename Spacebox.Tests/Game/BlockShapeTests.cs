using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Tests;

public class BlockShapeTests
{
    private const float Eps = 1e-4f;

    private static BlockGeometry TorchGeometry() =>
        new BlockGeometryBuilder().AddBox(new Vector3(-0.2f, 0f, -0.2f), new Vector3(0.2f, 0.8f, 0.2f)).Build();

    private static void AssertVector(Vector3 expected, Vector3 actual)
    {
        Assert.InRange(actual.X, expected.X - Eps, expected.X + Eps);
        Assert.InRange(actual.Y, expected.Y - Eps, expected.Y + Eps);
        Assert.InRange(actual.Z, expected.Z - Eps, expected.Z + Eps);
    }

    [Fact]
    public void Builder_AddBox_CreatesSixQuadsWithBounds()
    {
        var geometry = TorchGeometry();

        Assert.Equal(24, geometry.VertexCount);
        Assert.Equal(36, geometry.Indices.Length);
        AssertVector(new Vector3(-0.2f, 0f, -0.2f), geometry.Min);
        AssertVector(new Vector3(0.2f, 0.8f, 0.2f), geometry.Max);
    }

    [Fact]
    public void Builder_AddBox_TrianglesFaceOutwards()
    {
        var geometry = TorchGeometry();
        var center = (geometry.Min + geometry.Max) * 0.5f;

        for (int i = 0; i < geometry.Indices.Length; i += 3)
        {
            var a = geometry.Positions[geometry.Indices[i]];
            var b = geometry.Positions[geometry.Indices[i + 1]];
            var c = geometry.Positions[geometry.Indices[i + 2]];
            var normal = Vector3.Cross(b - a, c - a);
            var outwards = (a + b + c) / 3f - center;
            Assert.True(Vector3.Dot(normal, outwards) > 0f);
        }
    }

    [Fact]
    public void Geometry_RejectsIndicesOutOfRange()
    {
        Assert.Throws<ArgumentException>(() => new BlockGeometry(
            new[] { Vector3.Zero, Vector3.One, Vector3.UnitX },
            new[] { Vector3.UnitY, Vector3.UnitY, Vector3.UnitY },
            new[] { Vector2.Zero, Vector2.Zero, Vector2.Zero },
            new[] { 0, 1, 3 }));
    }

    [Fact]
    public void Obj_FromInterleaved_RestoresTextureVAndNormalizesNormals()
    {
        float[] vertices =
        {
            0f, 0f, 0f, 0f, 2f, 0f, 0.25f, 0.75f,
            1f, 0f, 0f, 0f, 0f, 0f, 1f, 1f,
            0f, 1f, 0f, 0f, 0f, 3f, 0f, 0f
        };

        var geometry = ObjBlockGeometry.FromInterleaved(vertices, new[] { 0, 1, 2 });

        Assert.Equal(new Vector2(0.25f, 0.25f), geometry.TexCoords[0]);
        Assert.Equal(new Vector2(1f, 0f), geometry.TexCoords[1]);
        AssertVector(Vector3.UnitY, geometry.Normals[0]);
        AssertVector(Vector3.UnitY, geometry.Normals[1]);
        AssertVector(Vector3.UnitZ, geometry.Normals[2]);
    }

    [Fact]
    public void Shape_Up_SupportedRestsOnBottomFace()
    {
        var shape = new BlockShape(TorchGeometry());
        var pose = shape.GetPose(Direction.Up, Rotation.None, true);

        AssertVector(new Vector3(0.3f, 0f, 0.3f), pose.Min);
        AssertVector(new Vector3(0.7f, 0.8f, 0.7f), pose.Max);
        Assert.Equal(new Engine.Vector3SByte(0, -1, 0), shape.GetSupportOffset(Direction.Up, Rotation.None));
    }

    [Fact]
    public void Shape_Up_UnsupportedIsCentered()
    {
        var shape = new BlockShape(TorchGeometry());
        var pose = shape.GetPose(Direction.Up, Rotation.None, false);

        AssertVector(new Vector3(0.3f, 0.1f, 0.3f), pose.Min);
        AssertVector(new Vector3(0.7f, 0.9f, 0.7f), pose.Max);
    }

    [Theory]
    [InlineData(Direction.Right, -1, 0, 0)]
    [InlineData(Direction.Left, 1, 0, 0)]
    [InlineData(Direction.Forward, 0, 0, -1)]
    [InlineData(Direction.Back, 0, 0, 1)]
    [InlineData(Direction.Down, 0, 1, 0)]
    public void Shape_OtherDirections_RestOnOppositeNeighbor(Direction direction, int sx, int sy, int sz)
    {
        var shape = new BlockShape(TorchGeometry());
        var support = shape.GetSupportOffset(direction, Rotation.None);
        Assert.Equal(new Engine.Vector3SByte(sx, sy, sz), support);

        var pose = shape.GetPose(direction, Rotation.None, true);
        int axis = sx != 0 ? 0 : sy != 0 ? 1 : 2;
        int sign = sx + sy + sz;
        float touchingFace = sign < 0 ? pose.Min[axis] : pose.Max[axis];
        float expectedFace = sign < 0 ? 0f : 1f;

        Assert.InRange(touchingFace, expectedFace - Eps, expectedFace + Eps);
        Assert.InRange((pose.Max - pose.Min)[axis], 0.8f - Eps, 0.8f + Eps);
    }

    [Fact]
    public void Shape_VertexFaces_FollowModelNormals()
    {
        var shape = new BlockShape(TorchGeometry());
        Assert.Contains(Face.Up, shape.VertexFaces);
        Assert.Contains(Face.Down, shape.VertexFaces);
        Assert.Contains(Face.Forward, shape.VertexFaces);
        Assert.Equal(4, shape.VertexFaces.Count(f => f == Face.Up));
    }

    [Fact]
    public void Resolver_UsesCodeGeometryAndCachesShapes()
    {
        var resolver = new BlockShapeResolver();
        var first = resolver.Resolve("slab");

        Assert.NotNull(first);
        Assert.Same(first, resolver.Resolve("SLAB"));
        Assert.Null(resolver.Resolve(""));
    }

    [Fact]
    public void Resolver_LoadsModelFromGameSetFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), "spacebox_shapes_" + Guid.NewGuid().ToString("N"));
        var folder = Path.Combine(root, BlockShapeResolver.ModelsFolder);
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "tri.obj"),
                "v 0 0 0\nv 1 0 0\nv 0 1 0\nvt 0 0\nvt 1 0\nvt 0 1\nvn 0 0 1\nf 1/1/1 2/2/1 3/3/1\n");

            var shape = new BlockShapeResolver(root).Resolve("tri");

            Assert.NotNull(shape);
            Assert.Equal(3, shape!.Geometry.VertexCount);
            Assert.Null(new BlockShapeResolver(root).Resolve("missing"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void DefaultTorchModel_Is04By08By04()
    {
        var path = FindRepoFile(Path.Combine("Spacebox", "Mods", "GameSets", "Default", "Models", "Blocks", "torch.obj"));
        var geometry = ObjBlockGeometry.Load(path);

        AssertVector(new Vector3(0.4f, 0.8f, 0.4f), geometry.Size);
        Assert.Equal(0f, geometry.Min.Y, 4);
    }

    [Fact]
    public void Block_HasCustomShape_DoesNotTouchOtherFields()
    {
        var block = new Block(5, Direction.Left, 7, 9, true, false, true, false);
        block.Rotation = Rotation.Half;
        block.HasCustomShape = true;

        Assert.True(block.HasCustomShape);
        Assert.Equal(5, block.Id);
        Assert.Equal(Direction.Left, block.Direction);
        Assert.Equal(Rotation.Half, block.Rotation);
        Assert.False(block.IsMeshHidden);
        Assert.False(block.HasMultiblockOffset);

        block.HasCustomShape = false;
        Assert.False(block.HasCustomShape);
        Assert.Equal(7, block.Mass);
    }

    [Theory]
    [InlineData(1, 0, 0, Direction.Right)]
    [InlineData(-1, 0, 0, Direction.Left)]
    [InlineData(0, 1, 0, Direction.Up)]
    [InlineData(0, -1, 0, Direction.Down)]
    [InlineData(0, 0, 1, Direction.Forward)]
    [InlineData(0, 0, -1, Direction.Back)]
    public void Placement_ShapedBlock_StandsOnClickedSurface(int nx, int ny, int nz, Direction expected)
    {
        var data = new Spacebox.Game.Resource.BlockData("Torch", "block") { Shape = new BlockShape(TorchGeometry()) };

        BlockPlacementHelper.CalculateOrientation(new Vector3(5, 7, 3), Vector3.Zero, Vector3.UnitY, new Vector3(nx, ny, nz),
            true, true, data, Rotation.Left, out var direction, out var rotation);

        Assert.Equal(expected, direction);
        Assert.Equal(Rotation.None, rotation);
        Assert.Equal(new Engine.Vector3SByte(-nx, -ny, -nz), data.Shape!.GetSupportOffset(direction, rotation));
    }

    [Theory]
    [InlineData(Rotation.None, Direction.Up)]
    [InlineData(Rotation.Right, Direction.Left)]
    [InlineData(Rotation.Half, Direction.Down)]
    [InlineData(Rotation.Left, Direction.Right)]
    public void Placement_ShapedBlockInAir_RotationTurnsThroughPlayerView(Rotation selected, Direction expected)
    {
        var data = new Spacebox.Game.Resource.BlockData("Torch", "block") { Shape = new BlockShape(TorchGeometry()) };

        BlockPlacementHelper.CalculateOrientation(new Vector3(5, 7, 1), Vector3.Zero, Vector3.UnitY, Vector3.Zero,
            false, false, data, selected, out var direction, out var rotation);

        Assert.Equal(expected, direction);
        Assert.Equal(Rotation.None, rotation);
    }

    [Fact]
    public void Placement_ShapedBlockInAir_FollowsPlayerUp()
    {
        var data = new Spacebox.Game.Resource.BlockData("Torch", "block") { Shape = new BlockShape(TorchGeometry()) };

        BlockPlacementHelper.CalculateOrientation(new Vector3(0, 0, 5), Vector3.Zero, new Vector3(0.9f, 0.3f, 0f), Vector3.Zero,
            false, false, data, Rotation.None, out var direction, out _);

        Assert.Equal(Direction.Right, direction);
    }

    [Fact]
    public void PreviewMesh_IsCenteredOnCellAndMapsUVs()
    {
        var shape = new BlockShape(TorchGeometry());
        var data = new Spacebox.Game.Resource.BlockData("Torch", "block") { Shape = shape };
        var pose = shape.GetPose(Direction.Up, Rotation.None, true);

        var (vertices, indices) = BlockShapePreviewMesh.Build(data, shape, pose, Spacebox.Game.Resource.BlockState.Active);

        Assert.Equal(shape.Geometry.VertexCount * 8, vertices.Length);
        Assert.Equal(shape.Geometry.Indices.Length, indices.Length);
        float minY = float.MaxValue;
        for (int i = 1; i < vertices.Length; i += 8) minY = MathF.Min(minY, vertices[i]);
        Assert.Equal(-0.5f, minY, 4);
    }

    [Fact]
    public void Texturing_MapToFace_InterpolatesFaceCorners()
    {
        var face = new[] { new Vector2(0.5f, 0.25f), new Vector2(0.75f, 0.25f), new Vector2(0.75f, 0.5f), new Vector2(0.5f, 0.5f) };

        Assert.Equal(new Vector2(0.625f, 0.375f), BlockShapeTexturing.MapToFace(face, new Vector2(0.5f, 0.5f)));
        Assert.Equal(face[2], BlockShapeTexturing.MapToFace(face, Vector2.One));
    }

    private static string FindRepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException(relative);
    }
}
