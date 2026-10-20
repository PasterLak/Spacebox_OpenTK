using Engine.Graphics.Atlas;

namespace Engine.Tests.Graphics;

public class TextureAtlasIgnoredFolderTests
{
    private static readonly string Root = Path.Combine("GameSets", "Default", "Blocks");

    [Theory]
    [InlineData("stone.png", false)]
    [InlineData("Walls/stone.png", false)]
    [InlineData("_Ignored/stone.png", true)]
    [InlineData("Walls/_old/stone.png", true)]
    [InlineData("_stone.png", false)]
    public void IsInIgnoredFolder_ChecksFoldersBelowTheRoot(string relative, bool expected)
    {
        string file = Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));

        Assert.Equal(expected, TextureAtlas.IsInIgnoredFolder(Root, file));
    }
}
