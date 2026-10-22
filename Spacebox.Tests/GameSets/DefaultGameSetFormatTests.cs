using System.Text.Json.Nodes;
using Spacebox.Game.Resource.Content;
using Spacebox.Game.Resource.Content.Conversion;

namespace Spacebox.Tests.GameSets;

public sealed class DefaultGameSetFormatTests
{
    private const string RegenerateHint = "If Default content was changed on purpose, regenerate the snapshot: dotnet run --project Tools/Spacebox.GameSetTool -- snapshot Spacebox/GameSets/Default Spacebox.Tests/Fixtures/GameSets/Default.snapshot.json";

    private static string SnapshotFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Spacebox.Tests", "Fixtures", "GameSets", "Default.snapshot.json");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Default.snapshot.json");
    }

    [Fact]
    public void Default_UsesTheFolderFormat()
    {
        Assert.False(GameSetFolderConverter.HasFlatFiles(DefaultGameSetContent.Folder));
        Assert.True(File.Exists(Path.Combine(DefaultGameSetContent.Folder, "Blocks", "order.json")));
        Assert.True(File.Exists(Path.Combine(DefaultGameSetContent.Folder, "Items", "order.json")));
        Assert.Empty(DefaultGameSetContent.Content.Warnings);
    }

    [Fact]
    public void Default_LoadsTheSameContentAsBeforeTheFolderConversion()
    {
        var expected = (JsonObject)JsonNode.Parse(File.ReadAllText(SnapshotFile()))!;
        var actual = ContentSnapshot.Create(DefaultGameSetContent.Content, DefaultGameSetContent.Assets);

        var differences = ContentSnapshot.Differences(expected, actual);

        Assert.True(differences.Count == 0, string.Join("\n", differences) + "\n" + RegenerateHint);
    }

    [Fact]
    public void Default_LoadsTheSameContentFromTheFlatFormat()
    {
        string copy = Path.Combine(Path.GetTempPath(), "spacebox_default_" + Guid.NewGuid().ToString("N"), "Default");
        try
        {
            GameSetFolderConverter.CopyDirectory(DefaultGameSetContent.Folder, copy);
            var folder = ContentSnapshot.Create(copy);

            GameSetFolderConverter.ExportFlatInPlace(copy);
            Assert.True(GameSetFolderConverter.HasFlatFiles(copy));
            Assert.Empty(ContentSnapshot.Differences(folder, ContentSnapshot.Create(copy)));

            var result = new ConversionResult();
            GameSetFolderConverter.ConvertInPlace(copy, null, result);
            Assert.Empty(result.Errors);
            Assert.Empty(ContentSnapshot.Differences(folder, ContentSnapshot.Create(copy)));
        }
        finally
        {
            var parent = Path.GetDirectoryName(copy)!;
            if (Directory.Exists(parent)) Directory.Delete(parent, true);
        }
    }

    [Fact]
    public void Default_EntryFoldersHoldTheirOwnTextures()
    {
        var analyzer = DefaultGameSetContent.Content.FindBlock("analyzer");

        Assert.NotNull(analyzer);
        Assert.Equal("analyzer", Path.GetFileName(analyzer.SearchDirectories[0]));
        Assert.StartsWith(analyzer.SearchDirectories[0], DefaultGameSetContent.Assets.BlockTextures["analyzer"]);
        Assert.StartsWith(analyzer.SearchDirectories[0], DefaultGameSetContent.Assets.BlockEmissions["analyzer"]);
    }
}
