using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.GameSets;

public sealed class EntryFilesTests
{
    [Theory]
    [InlineData("Blocks/Rocks/stone/stone.json", true)]
    [InlineData("Blocks/Rocks/stone/STONE.json", true)]
    [InlineData("Blocks/Rocks/stone/block.json", true)]
    [InlineData("Blocks/Rocks/stone/blueprint.json", false)]
    [InlineData("Blocks/Rocks/stone/iron.json", false)]
    [InlineData("Blocks/blueprint/blueprint.json", false)]
    [InlineData("Blocks/order.json", false)]
    public void EntryFile_IsTheDefaultNameOrTheFolderName(string path, bool expected)
    {
        Assert.Equal(expected, EntryFiles.IsEntryFile(path, GameSetFormat.BlockEntryFile, _ => false));
    }

    [Fact]
    public void DefaultName_IsIgnoredNextToANamedFile()
    {
        Assert.False(EntryFiles.IsEntryFile("Blocks/stone/block.json", GameSetFormat.BlockEntryFile, path => path == "Blocks/stone/stone.json"));
    }

    [Fact]
    public void NewEntryFiles_AreNamedAfterTheirFolder()
    {
        string directory = Path.Combine(Path.GetTempPath(), "spacebox_entry_" + Guid.NewGuid().ToString("N"), "lamp");

        Assert.Equal(Path.Combine(directory, "lamp.json"), EntryFiles.PathFor(directory, GameSetFormat.BlockEntryFile));
        Assert.Null(EntryFiles.Find(directory, GameSetFormat.BlockEntryFile));
    }
}
