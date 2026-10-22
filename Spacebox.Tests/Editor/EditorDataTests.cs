using System.Text.Json.Nodes;
using Spacebox.Editor.Data;
using Spacebox.Game.Resource;
using Spacebox.Tests.GameSets;
using Xunit;

namespace Spacebox.Tests.Editor;

public sealed class EditorDataTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    [Fact]
    public void SourceFolder_IsFoundNextToTheProjectFile()
    {
        _sets.Write("repo/Spacebox/Spacebox.csproj", "<Project />");
        _sets.Folder("repo/Spacebox/Mods/GameSets/Default");
        string bin = _sets.Folder("repo/Spacebox/bin/Debug/net8.0");
        string installed = _sets.Folder("repo/Spacebox/bin/Debug/net8.0/Mods/GameSets/Default");
        string downloaded = _sets.Folder("repo/Spacebox/bin/Debug/net8.0/Multiplayer/Mods/server/Default");

        string source = DevelopmentSources.FindSourceFolder(installed, bin);

        Assert.Equal(Path.GetFullPath(Path.Combine(_sets.Root, "repo", "Spacebox", "Mods", "GameSets", "Default")), source);
        Assert.Null(DevelopmentSources.FindSourceFolder(downloaded, bin));
        Assert.Null(DevelopmentSources.FindSourceFolder(Path.Combine(_sets.Root, "repo", "Spacebox", "Mods", "GameSets", "Default"), Path.Combine(_sets.Root, "repo", "Spacebox")));
        Assert.Null(DevelopmentSources.FindSourceFolder(_sets.Folder("elsewhere/Mods/GameSets/Default"), _sets.Folder("elsewhere")));
    }

    [Fact]
    public void Mirror_WritesAndDeletesTheSourceCopy()
    {
        var mirror = new SourceFolderMirror(_sets.Folder("source"));

        mirror.Write("Blocks/lamp/lamp.json", "{}");
        Assert.True(File.Exists(Path.Combine(mirror.Root, "Blocks", "lamp", "lamp.json")));

        mirror.Delete("Blocks/lamp/lamp.json");
        Assert.False(File.Exists(Path.Combine(mirror.Root, "Blocks", "lamp", "lamp.json")));
    }

    [Fact]
    public void Project_SaveAlsoWritesTheMirror()
    {
        _sets.Write("Mod/Crafting/categories.json", "{ \"Categories\": [ { \"Id\": \"a\" }, { \"Id\": \"b\" } ] }");
        var project = new GameSetProject(Path.Combine(_sets.Root, "Mod"), "mod") { Mirror = new SourceFolderMirror(_sets.Folder("source")) };
        var source = new ArrayEntrySource(project, "Crafting/categories.json", "Categories", GameSetCatalog.CraftingCategories);

        var entries = source.GetEntries();
        Assert.False(source.CanMove(entries[0], -1));
        Assert.True(source.CanMove(entries[0], 1));
        source.Move(entries[0], 1);
        project.SaveAll();

        var saved = JsonNode.Parse(File.ReadAllText(Path.Combine(_sets.Root, "source", "Crafting", "categories.json")))!;
        Assert.Equal("b", saved["Categories"]![0]!["Id"]!.GetValue<string>());
        Assert.Equal("a", saved["Categories"]![1]!["Id"]!.GetValue<string>());
    }

    [Fact]
    public void SoundUsage_ListsTheFilesThatNameASound()
    {
        var files = new List<(string, JsonNode)>
        {
            ("Blocks/door/door.json", JsonNode.Parse("{ \"Door\": { \"OpenSound\": \"doorOpen\" }, \"Name\": \"Door\" }")!),
            ("Items/gun/gun.json", JsonNode.Parse("{ \"ShotSound\": \"DOOROPEN\", \"Tags\": [ \"shot\" ] }")!)
        };

        var index = SoundUsageIndex.Build(files, new[] { "doorOpen", "shot", "unused" });

        Assert.Equal(new[] { "Blocks/door/door.json", "Items/gun/gun.json" }, index.UsagesOf("doorOpen"));
        Assert.Equal(new[] { "Items/gun/gun.json" }, index.UsagesOf("shot"));
        Assert.Empty(index.UsagesOf("unused"));
    }
}
