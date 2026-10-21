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
        var data = new SignBlockData(new BlockData("Sign", "sign") { Size = new Vector3Byte(9, 1, 1) }, new SignJSON
        {
            MaxLines = 99, MaxLineLength = 0, TextColor = "bad", Align = "middle", PixelsPerBlock = 4000, DefaultText = "a\nb"
        });

        Assert.Equal(SignBlockData.MaxWidth, data.Width);
        Assert.Equal(16, data.MaxLines);
        Assert.Equal(1, data.MaxLineLength);
        Assert.Equal(256, data.PixelsPerBlock);
        Assert.Equal(SignAlign.Center, data.Align);
        Assert.Equal("a\nb", data.DefaultText);
    }

    [Fact]
    public void BlockData_DefaultsToCenterAlignment()
    {
        var data = new SignBlockData(new BlockData("Sign", "sign"), new SignJSON());
        Assert.Equal(SignAlign.Center, data.Align);
        Assert.Equal(1, data.Width);
    }

    [Fact]
    public void WideSign_SecondCellLiesAlongTheTextInThePlate()
    {
        var shape = new BlockShape(SignPlateGeometry.Create());
        var data = new BlockData("Wide", "sign") { Size = new Vector3Byte(2, 1, 1), BaseFrontDirection = Direction.Up };
        var cells = new List<MultiblockCell>();

        foreach (Direction direction in Enum.GetValues<Direction>())
        {
            foreach (Rotation rotation in Enum.GetValues<Rotation>())
            {
                MultiblockLayout.GetCells(data, direction, rotation, cells);
                Assert.Equal(2, cells.Count);
                Assert.True(cells[0].IsOrigin);

                var offset = new Vector3(cells[1].Offset.X, cells[1].Offset.Y, cells[1].Offset.Z);
                var right = Vector3.TransformVector(SignPlateGeometry.TextRight, shape.GetTransform(direction, rotation));
                Assert.True((offset - right).Length < 1e-4f, $"{direction} {rotation}: {offset} vs {right}");
            }
        }
    }

    [Fact]
    public void Document_RoundTripsMarkup()
    {
        string markup = "Hello <b>big</b>\n<right><color=#ff0000>Red</color> plain";
        var document = SignDocument.FromMarkup(markup, 4, 15, SignAlign.Center);

        Assert.Equal(2, document.Lines.Count);
        Assert.Equal(SignAlign.Center, document.Lines[0].Align);
        Assert.Equal(SignAlign.Right, document.Lines[1].Align);
        Assert.True(document.Lines[0].Chars[6].Bold);
        Assert.Null(document.Lines[1].Chars[4].Color);
        Assert.Equal(markup, document.ToMarkup());
    }

    [Fact]
    public void Document_TypingUsesCurrentStyleAndRespectsLimits()
    {
        var document = new SignDocument(2, 5, SignAlign.Center);
        document.ToggleBold();
        document.Insert("abcdefg");
        document.NewLine();
        document.ApplyColor(new Color4(1f, 0f, 0f, 1f));
        document.Insert("x\ny\nz");

        Assert.Equal("abcde\nxyz", document.PlainText());
        Assert.Equal("<b>abcde</b>\n<color=#ff0000><b>xyz</b></color>", document.ToMarkup());
    }

    [Fact]
    public void Document_StylesSelectionAndResets()
    {
        var document = SignDocument.FromMarkup("one two", 4, 15, SignAlign.Center);
        document.SetCaret(new SignCaret(0, 4), false);
        document.SetCaret(new SignCaret(0, 7), true);
        document.ToggleBold();
        document.SetAlign(SignAlign.Left);
        Assert.Equal("<left>one <b>two</b>", document.ToMarkup());

        document.SelectAll();
        document.ResetStyle();
        Assert.Equal("one two", document.ToMarkup());
    }

    [Fact]
    public void Document_EditsAcrossLines()
    {
        var document = SignDocument.FromMarkup("ab\ncd", 4, 15, SignAlign.Center);
        document.SetCaret(new SignCaret(1, 0), false);
        document.Backspace();
        Assert.Equal("abcd", document.PlainText());

        document.SetCaret(new SignCaret(0, 1), false);
        document.SetCaret(new SignCaret(0, 3), true);
        Assert.Equal("bc", document.SelectedText());
        document.Insert("X");
        Assert.Equal("aXd", document.PlainText());
    }

    [Fact]
    public void Document_EscapesTextThatLooksLikeATag()
    {
        var document = new SignDocument(1, 15, SignAlign.Center);
        document.Insert("2<3 <b>");
        string markup = document.ToMarkup();

        var lines = SignMarkup.Parse(markup, Black, SignAlign.Center);
        Assert.Single(lines[0].Runs);
        Assert.False(lines[0].Runs[0].Bold);
        Assert.StartsWith("2<3 ", lines[0].Runs[0].Text);
    }

    [Theory]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    [InlineData(Direction.Forward)]
    [InlineData(Direction.Back)]
    public void Placement_KeepsTextUprightOnWalls(Direction wall)
    {
        var shape = new BlockShape(SignPlateGeometry.Create());
        var rotation = shape.BestRotation(wall, Vector3.UnitY);
        var textUp = Vector3.TransformVector(SignPlateGeometry.TextUp, shape.GetTransform(wall, rotation));

        Assert.Equal(1f, Vector3.Dot(textUp, Vector3.UnitY), 3);
    }

    [Fact]
    public void Placement_FloorSignFacesThePlayer()
    {
        var shape = new BlockShape(SignPlateGeometry.Create());
        var away = new Vector3(0f, 0f, -1f);
        var rotation = shape.BestRotation(Direction.Up, away);
        var textUp = Vector3.TransformVector(SignPlateGeometry.TextUp, shape.GetTransform(Direction.Up, rotation));

        Assert.Equal(1f, Vector3.Dot(textUp, away), 3);
    }

    [Fact]
    public void Plate_IsThinAndRestsOnTheSupport()
    {
        var shape = new BlockShape(SignPlateGeometry.Create());
        var pose = shape.GetPose(Direction.Up, Rotation.None, true);

        Assert.Equal(0f, pose.Min.Y, 3);
        Assert.Equal(SignPlateGeometry.Thickness, pose.Max.Y, 3);
        Assert.True(shape.RequiresSupport);
        Assert.True(shape.HasFacing);
    }

    [Fact]
    public void TextSize_ReadsAndWritesTags()
    {
        Assert.Equal(SignTextSize.Default, SignMarkup.ReadTextSize("plain"));
        Assert.True(SignMarkup.ReadTextSize("<fill>SHOP").Fill);
        Assert.Equal(2.5f, SignMarkup.ReadTextSize("<size=2.5>Hi").Scale);
        Assert.Equal(SignTextSize.MaxScale, SignMarkup.ReadTextSize("<size=999>Hi").Scale);
        Assert.Equal("Hi", SignMarkup.StripTags("<size=2>Hi"));

        var document = SignDocument.FromMarkup("<size=3>A<b>B</b>", 4, 15, SignAlign.Center);
        Assert.Equal(3f, document.TextSize.Scale);
        Assert.Equal("<size=3>A<b>B</b>", document.ToMarkup());

        document.TextSize = new SignTextSize(1f, true);
        Assert.StartsWith("<fill>", document.ToMarkup());
        document.ResetStyle();
        Assert.Equal("AB", document.ToMarkup());
    }

    [Fact]
    public void TextSize_NeverExceedsTheFit()
    {
        Assert.Equal(2f, new SignTextSize(5f, false).Resolve(2f));
        Assert.Equal(1.5f, new SignTextSize(1.5f, false).Resolve(4f));
        Assert.Equal(4f, new SignTextSize(1f, true).Resolve(4f));
        Assert.Equal(0.5f, SignTextSize.Default.Resolve(0.5f));
    }

    [Theory]
    [InlineData("<fill>SHOP")]
    [InlineData("<size=16>SHOP")]
    [InlineData("a very long line that is too long\nsecond")]
    [InlineData("<fill><b>1\n2\n3\n4\n5\n6</b>")]
    public void Baker_KeepsTextInsideTheArea(string markup)
    {
        const int width = 192, height = 96, margin = 4;
        var lines = SignMarkup.Parse(markup, Black, SignAlign.Center);
        var pixels = SignTextBaker.Bake(lines, width, height, margin, "missing.ttf", SignMarkup.ReadTextSize(markup));

        var (minX, minY, maxX, maxY) = Bounds(pixels.Data, width, height);
        Assert.True(maxX >= minX, "nothing was drawn");
        Assert.True(minX >= margin && minY >= margin && maxX < width - margin && maxY < height - margin, $"{minX},{minY} - {maxX},{maxY}");
    }

    [Fact]
    public void Baker_FillMakesShortTextBig()
    {
        const int width = 192, height = 96, margin = 4;
        var plain = SignMarkup.Parse("SHOP", Black, SignAlign.Center);
        float fit = SignTextBaker.FitScale(plain, width, height, margin, "missing.ttf");
        Assert.True(fit > 3f, $"fit {fit}");

        var pixels = SignTextBaker.Bake(plain, width, height, margin, "missing.ttf", new SignTextSize(1f, true));
        var (minX, minY, maxX, maxY) = Bounds(pixels.Data, width, height);
        Assert.True(maxX - minX > (width - margin * 2) * 0.8f || maxY - minY > (height - margin * 2) * 0.5f, $"{minX},{minY} - {maxX},{maxY}");
    }

    private static (int, int, int, int) Bounds(byte[] data, int width, int height)
    {
        int minX = width, minY = height, maxX = -1, maxY = -1;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (data[(y * width + x) * 4 + 3] == 0) continue;
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            }
        return (minX, minY, maxX, maxY);
    }
}
