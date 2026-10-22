using Engine.GUI;

namespace Engine.Tests;

public class ImGuiFontScaleTests
{
    [Theory]
    [InlineData(800, 1f)]
    [InlineData(1024, 1f)]
    [InlineData(1280, 1f)]
    [InlineData(1366, 1f)]
    [InlineData(1600, 1.25f)]
    [InlineData(1920, 1.5f)]
    [InlineData(2560, 2f)]
    [InlineData(3840, 3f)]
    public void Scale_NeverShrinksThePixelFontAndSnapsToSteps(int width, float expected)
    {
        Assert.Equal(expected, ImGuiFontScale.For(width), 4);
    }

    [Fact]
    public void WithoutStep_ScalesWithTheWidthAboveTheMinimum()
    {
        Assert.Equal(1.1f, ImGuiFontScale.For(1408, 0f), 4);
        Assert.Equal(0.5f, ImGuiFontScale.For(640, 0f, 0f), 4);
    }
}
