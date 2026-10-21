using Engine.Automation;

namespace Engine.Tests.Automation;

public class GlyphTextAssemblerTests
{
    private const float Advance = 6f;
    private const float Height = 12f;

    private static void Write(GlyphTextAssembler assembler, string text, float x, float y)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ') continue;
            assembler.Add(new GlyphPlacement(text[i], x + i * Advance, y, Advance, Height, Advance));
        }
    }

    [Fact]
    public void Joins_GlyphsOfOneLine_AndRestoresSpaces()
    {
        var assembler = new GlyphTextAssembler();
        Write(assembler, "No,  thanks", 100, 50);

        var runs = assembler.Finish();

        var run = Assert.Single(runs);
        Assert.Equal("No,  thanks", run.Text);
        Assert.Equal(100f, run.X);
        Assert.Equal(50f, run.Y);
        Assert.Equal(11 * Advance, run.Width);
        Assert.Equal(Height, run.Height);
    }

    [Fact]
    public void Splits_OnNewLineAndOnLargeGap()
    {
        var assembler = new GlyphTextAssembler();
        Write(assembler, "Start", 100, 50);
        Write(assembler, "Cancel", 300, 50);
        Write(assembler, "Next line", 100, 62);

        var runs = assembler.Finish();

        Assert.Equal(new[] { "Start", "Cancel", "Next line" }, runs.Select(r => r.Text));
        Assert.Equal(300f, runs[1].X);
        Assert.Equal(62f, runs[2].Y);
    }

    [Fact]
    public void Drops_ShadowCopiesOfTheSameText()
    {
        var assembler = new GlyphTextAssembler();
        Write(assembler, "Craft", 101, 51);
        Write(assembler, "Craft", 100, 50);
        Write(assembler, "Craft", 100, 200);

        var runs = assembler.Finish();

        Assert.Equal(2, runs.Count);
        Assert.Equal(200f, runs[1].Y);
    }

    [Fact]
    public void Finish_ResetsForTheNextWindow()
    {
        var assembler = new GlyphTextAssembler();
        Write(assembler, "One", 0, 0);
        assembler.Finish();
        Write(assembler, "Two", 0, 0);

        Assert.Equal("Two", Assert.Single(assembler.Finish()).Text);
    }

    [Fact]
    public void Center_IsTheMiddleOfTheRun()
    {
        var run = new UiTextRun("Ok", 10, 20, 12, 12);

        Assert.Equal(16f, run.CenterX);
        Assert.Equal(26f, run.CenterY);
    }
}
