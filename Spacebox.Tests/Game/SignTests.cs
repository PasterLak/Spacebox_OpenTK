using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;
using Spacebox.Game.Resource;
using Spacebox.Game.Signs;

namespace Spacebox.Tests.Game;

public class SignTests
{
    private static readonly Color4 Black = new Color4(0f, 0f, 0f, 1f);

    [Fact]
    public void Markup_ParsesBoldColorsAndAlignment()
    {
        var lines = SignMarkup.Parse("<center>Hello <b>big</b>\n<right><color=red>Red <color=#00ff00>Green</color> red</color> plain", Black, SignAlign.Left);

        Assert.Equal(2, lines.Count);
        Assert.Equal(SignAlign.Center, lines[0].Align);
        Assert.Equal(new[] { "Hello ", "big" }, lines[0].Runs.Select(r => r.Text));
        Assert.False(lines[0].Runs[0].Bold);
        Assert.True(lines[0].Runs[1].Bold);

        Assert.Equal(SignAlign.Right, lines[1].Align);
        var runs = lines[1].Runs;
        Assert.Equal(new[] { "Red ", "Green", " red", " plain" }, runs.Select(r => r.Text));
        Assert.Equal(0f, runs[1].Color.R, 3);
        Assert.Equal(1f, runs[1].Color.G, 3);
        Assert.Equal(runs[0].Color, runs[2].Color);
        Assert.Equal(Black, runs[3].Color);
    }

    [Fact]
    public void Markup_UnknownTagsStayText()
    {
        var lines = SignMarkup.Parse("a <i>b</i> <color=nope>c 2<3", Black, SignAlign.Center);
        Assert.Equal("a <i>b</i> <color=nope>c 2<3", string.Concat(lines[0].Runs.Select(r => r.Text)));
    }

    [Fact]
    public void Markup_BoldCarriesOverLines()
    {
        var lines = SignMarkup.Parse("<b>one\ntwo</b> three", Black, SignAlign.Center);
        Assert.True(lines[1].Runs[0].Bold);
        Assert.False(lines[1].Runs[1].Bold);
    }

    [Fact]
    public void Sanitize_LimitsLinesAndVisibleLengthAndKeepsTags()
    {
        string text = "<b>12345678</b>\nline2\nline3\x07\nline4";
        string sanitized = SignMarkup.Sanitize(text, 3, 5);

        Assert.Equal("<b>12345</b>\nline2\nline3", sanitized);
        Assert.Equal(5, SignMarkup.VisibleLength("<color=red>abcde</color>"));
        Assert.Equal("abc", SignMarkup.StripTags("<b>a</b><color=#fff>bc</color>"));
    }

    [Theory]
    [InlineData("#ff8800", 1f, 0.533f, 0f)]
    [InlineData("#f80", 1f, 0.533f, 0f)]
    [InlineData("white", 1f, 1f, 1f)]
    public void Colors_ParseHexAndNames(string value, float r, float g, float b)
    {
        Assert.True(SignMarkup.TryParseColor(value, out var color));
        Assert.Equal(r, color.R, 2);
        Assert.Equal(g, color.G, 2);
        Assert.Equal(b, color.B, 2);
    }

    [Fact]
    public void Colors_RejectInvalid()
    {
        Assert.False(SignMarkup.TryParseColor("#12345", out _));
        Assert.False(SignMarkup.TryParseColor("rainbow", out _));
        Assert.False(SignMarkup.TryParseColor("", out _));
    }

    [Fact]
    public void BlockData_ClampsSettings()
    {
        var data = new SignBlockData(new BlockData("Sign", "sign"), new SignJSON
        {
            Width = 5, MaxLines = 99, MaxLineLength = 0, TextColor = "bad", Align = "middle", PixelsPerBlock = 4000, DefaultText = "a\nb"
        });

        Assert.Equal(2, data.Width);
        Assert.Equal(16, data.MaxLines);
        Assert.Equal(1, data.MaxLineLength);
        Assert.Equal(256, data.PixelsPerBlock);
        Assert.Equal(SignAlign.Center, data.Align);
        Assert.Equal("a\nb", data.DefaultText);
    }

    [Fact]
    public void WideSign_ReservesOneNeighbourInThePlate()
    {
        var shape = new BlockShape(SignPlateGeometry.Create(2));
        var cells = new List<Vector3i>();

        foreach (Direction direction in Enum.GetValues<Direction>())
        {
            var normal = BlockRotationHelper.CalculateTransformMatrix(Direction.Up, direction, Rotation.None);
            var up = Vector3.TransformVector(Vector3.UnitY, normal);
            foreach (Rotation rotation in Enum.GetValues<Rotation>())
            {
                SignBlockData.GetExtraCells(2, direction, rotation, cells);
                Assert.Single(cells);
                var offset = new Vector3(cells[0].X, cells[0].Y, cells[0].Z);
                Assert.Equal(1f, offset.Length, 3);
                Assert.Equal(0f, Vector3.Dot(offset, up), 3);

                var origin = shape.ToCellSpace(new Vector3(0f, 0f, 0f), direction, rotation, true);
                var far = shape.ToCellSpace(new Vector3(1f, 0f, 0f), direction, rotation, true);
                Assert.True((offset - (far - origin)).Length < 1e-4f);
            }
        }

        SignBlockData.GetExtraCells(1, Direction.Up, Rotation.None, cells);
        Assert.Empty(cells);
    }

    [Theory]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    [InlineData(Direction.Forward)]
    [InlineData(Direction.Back)]
    public void Placement_KeepsTextUprightOnWalls(Direction wall)
    {
        var shape = new BlockShape(SignPlateGeometry.Create(1));
        var rotation = shape.BestRotation(wall, Vector3.UnitY);
        var textUp = Vector3.TransformVector(SignPlateGeometry.TextUp, shape.GetTransform(wall, rotation));

        Assert.Equal(1f, Vector3.Dot(textUp, Vector3.UnitY), 3);
    }

    [Fact]
    public void Placement_FloorSignFacesThePlayer()
    {
        var shape = new BlockShape(SignPlateGeometry.Create(1));
        var away = new Vector3(0f, 0f, -1f);
        var rotation = shape.BestRotation(Direction.Up, away);
        var textUp = Vector3.TransformVector(SignPlateGeometry.TextUp, shape.GetTransform(Direction.Up, rotation));

        Assert.Equal(1f, Vector3.Dot(textUp, away), 3);
    }

    [Fact]
    public void Plate_IsThinAndRestsOnTheSupport()
    {
        var shape = new BlockShape(SignPlateGeometry.Create(1));
        var pose = shape.GetPose(Direction.Up, Rotation.None, true);

        Assert.Equal(0f, pose.Min.Y, 3);
        Assert.Equal(SignPlateGeometry.Thickness, pose.Max.Y, 3);
        Assert.True(shape.RequiresSupport);
        Assert.True(shape.HasFacing);
    }
}
