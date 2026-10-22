using System.Text.Json;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;
using Spacebox.Game.Resource;

namespace Spacebox.Tests.Game;

public class PictureFrameTests
{
    private const float Epsilon = 0.0001f;

    [Theory]
    [InlineData(1, 1, 0, 0, PlateEdges.All)]
    [InlineData(2, 1, 0, 0, PlateEdges.Left | PlateEdges.Top | PlateEdges.Bottom)]
    [InlineData(2, 1, 1, 0, PlateEdges.Right | PlateEdges.Top | PlateEdges.Bottom)]
    [InlineData(2, 2, 1, 1, PlateEdges.Right | PlateEdges.Bottom)]
    [InlineData(3, 3, 1, 1, 0)]
    public void EdgeMask_MarksOuterEdgesOfTheStructure(int width, int height, int x, int z, int expected)
    {
        var mask = PlateEdges.Mask(new Vector3Byte(width, 1, height), new Vector3Byte(x, 0, z));
        Assert.Equal(expected, mask);
    }

    [Fact]
    public void Geometry_FullRimRisesAboveThePlate()
    {
        var geometry = PictureFrameGeometry.Create(PlateEdges.All);

        Assert.Equal(0f, geometry.Min.Y, 4);
        Assert.Equal(PictureFrameGeometry.Thickness + PictureFrameGeometry.RimHeight, geometry.Max.Y, 4);
        Assert.Equal(-0.5f, geometry.Min.X, 4);
        Assert.Equal(0.5f, geometry.Max.Z, 4);
        Assert.True(geometry.RequiresSupport);
        Assert.Equal(PictureFrameGeometry.PictureUp, geometry.FacingUp);
    }

    [Fact]
    public void Geometry_InnerCellIsAFlatPlate()
    {
        var geometry = PictureFrameGeometry.Create(0);

        Assert.Equal(PictureFrameGeometry.Thickness, geometry.Max.Y, 4);
        Assert.Equal(8, geometry.VertexCount);
    }

    [Fact]
    public void Geometry_RimTopsSampleTheRimPixelsOfTheFrontTexture()
    {
        var geometry = PictureFrameGeometry.Create(PlateEdges.All);
        float rimTop = PictureFrameGeometry.Thickness + PictureFrameGeometry.RimHeight;
        int checkedVertices = 0;

        for (int i = 0; i < geometry.VertexCount; i++)
        {
            if (MathF.Abs(geometry.Positions[i].Y - rimTop) > Epsilon || geometry.Normals[i].Y < 0.9f) continue;

            var position = geometry.Positions[i];
            var uv = geometry.TexCoords[i];
            Assert.Equal(position.X + 0.5f, uv.X, 4);
            Assert.Equal(0.5f - position.Z, uv.Y, 4);
            Assert.InRange(uv.X, PictureFrameGeometry.Margin - Epsilon, 1f - PictureFrameGeometry.Margin + Epsilon);
            Assert.InRange(uv.Y, PictureFrameGeometry.Margin - Epsilon, 1f - PictureFrameGeometry.Margin + Epsilon);
            checkedVertices++;
        }

        Assert.Equal(16, checkedVertices);
    }

    [Fact]
    public void Shape_PicksTheEdgeVariantPerCell()
    {
        var shape = PictureFrameGeometry.Shape;
        var size = new Vector3Byte(2, 1, 2);

        var corner = shape.ForCell(size, new Vector3Byte(0, 0, 0));
        var single = shape.ForCell(new Vector3Byte(1, 1, 1), new Vector3Byte(0, 0, 0));

        Assert.NotSame(corner, single);
        Assert.True(corner.Geometry.VertexCount < single.Geometry.VertexCount);
        Assert.Equal(shape.Geometry.VertexCount, single.Geometry.VertexCount);
    }

    [Fact]
    public void Shape_VariantsRestOnTheSameSupport()
    {
        var shape = PictureFrameGeometry.Shape;
        var full = shape.ForCell(new Vector3Byte(1, 1, 1), new Vector3Byte(0, 0, 0)).GetPose(Direction.Forward, Rotation.None, true);
        var inner = shape.ForCell(new Vector3Byte(3, 1, 3), new Vector3Byte(1, 0, 1)).GetPose(Direction.Forward, Rotation.None, true);

        Assert.Equal(full.Positions.Min(p => p.Z), inner.Positions.Min(p => p.Z), 4);
    }

    [Fact]
    public void Shape_WithoutVariantsIgnoresTheCell()
    {
        var shape = new BlockShape(SignPlateGeometry.Create());
        Assert.Same(shape, shape.ForCell(new Vector3Byte(2, 1, 1), new Vector3Byte(1, 0, 0)));
    }

    [Fact]
    public void BlockData_ClampsSizeAndQualifiesPictureIds()
    {
        var data = new PictureFrameBlockData(new BlockData("Frame", "frame"), new PictureFrameJSON
        {
            Width = 9,
            Height = 0,
            Pictures = new List<string> { "cactus", "other:moon", " ", "cactus" }
        }, "default");

        Assert.Equal(PictureFrameBlockData.MaxSize, data.Width);
        Assert.Equal(1, data.Height);
        Assert.Equal(new[] { "default:cactus", "other:moon" }, data.PictureIds.OrderBy(id => id));
    }

    [Fact]
    public void BlockData_AcceptsOnlyListedPictures()
    {
        var data = new PictureFrameBlockData(new BlockData("Frame", "frame"), new PictureFrameJSON { Pictures = new List<string> { "cactus" } }, "default");

        Assert.True(data.Accepts(new PictureItem("Cactus", 1f) { Id_string = "default:cactus" }));
        Assert.False(data.Accepts(new PictureItem("Moon", 1f) { Id_string = "default:moon" }));
        Assert.False(data.Accepts(new Item(1, "Cactus") { Id_string = "default:cactus" }));
        Assert.False(data.Accepts(null));
    }

    [Fact]
    public void Frame_InsertsSwapsAndTakesOutPictures()
    {
        var frame = new PictureFrameBlock(new PictureFrameBlockData(new BlockData("Frame", "frame"), new PictureFrameJSON { Pictures = new List<string> { "cactus", "moon" } }, "default"));
        var cactus = new PictureItem("Cactus", 1f) { Id = 10, Id_string = "default:cactus" };
        var moon = new PictureItem("Moon", 1f) { Id = 11, Id_string = "default:moon" };

        Assert.False(frame.TrySwap(null, out _));
        Assert.True(frame.TrySwap(cactus, out var removed));
        Assert.Null(removed);
        Assert.Same(cactus, frame.Picture);

        Assert.False(frame.TrySwap(cactus, out _));
        Assert.True(frame.TrySwap(moon, out removed));
        Assert.Same(cactus, removed);

        Assert.True(frame.TrySwap(null, out removed));
        Assert.Same(moon, removed);
        Assert.Null(frame.Picture);
    }

    [Fact]
    public void Frame_RefusesPicturesItDoesNotList()
    {
        var frame = new PictureFrameBlock(new PictureFrameBlockData(new BlockData("Frame", "frame"), new PictureFrameJSON { Pictures = new List<string> { "cactus" } }, "default"));
        var wide = new PictureItem("Wide", 1f) { Id = 12, Id_string = "default:wide" };

        Assert.False(frame.TrySwap(wide, out _));
        frame.RestorePicture(wide);
        Assert.Null(frame.Picture);
    }

    [Fact]
    public void PictureItem_DoesNotStack()
    {
        Assert.Equal(1, new PictureItem("Cactus", 1f).StackSize);
    }

    [Fact]
    public void DefaultFrames_ListExistingPicturesOfTheirSize()
    {
        var pictures = DefaultPictures();
        var framed = new HashSet<string>();
        int frames = 0;

        foreach (var block in Load("blocks.json").EnumerateArray())
        {
            if (!block.TryGetProperty("Type", out var type) || !string.Equals(type.GetString(), "frame", StringComparison.OrdinalIgnoreCase)) continue;
            frames++;

            var frame = block.GetProperty("Frame");
            int width = frame.GetProperty("Width").GetInt32();
            int height = frame.GetProperty("Height").GetInt32();
            foreach (var id in frame.GetProperty("Pictures").EnumerateArray().Select(p => p.GetString()))
            {
                Assert.True(pictures.TryGetValue(id, out var size), $"{block.GetProperty("ID").GetString()} lists unknown picture {id}");
                Assert.Equal((width, height), size);
                framed.Add(id);
            }
        }

        Assert.True(frames > 0);
        Assert.Empty(pictures.Keys.Except(framed));
    }

    [Fact]
    public void DefaultPictures_HaveImagesAndBlueprints()
    {
        var crafted = new HashSet<string?>(Load("crafting.json").GetProperty("Blueprints").EnumerateArray()
            .Select(b => b.GetProperty("Product").GetProperty("Item").GetString()));

        foreach (var item in Load("items.json").EnumerateArray())
        {
            if (!item.TryGetProperty("Type", out var type) || type.GetString() != "picture") continue;

            string id = item.GetProperty("ID").GetString();
            Assert.True(File.Exists(Path.Combine(GameSetFolder(), item.GetProperty("Picture").GetString())), $"{id} has no picture file");
            Assert.Contains(id, crafted);
        }
    }

    private static Dictionary<string, (int, int)> DefaultPictures()
    {
        var result = new Dictionary<string, (int, int)>();
        foreach (var item in Load("items.json").EnumerateArray())
        {
            if (!item.TryGetProperty("Type", out var type) || type.GetString() != "picture") continue;
            result[item.GetProperty("ID").GetString()] = (item.GetProperty("Width").GetInt32(), item.GetProperty("Height").GetInt32());
        }
        return result;
    }

    private static JsonElement Load(string name) => DefaultGameSetContent.Flat(name);

    private static string GameSetFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Spacebox", "Mods", "GameSets", "Default");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Default GameSet");
    }
}
