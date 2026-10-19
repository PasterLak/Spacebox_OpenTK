using System.Text.Json.Nodes;
using Spacebox.Editor.Data;

namespace Spacebox.Tests.Editor;

public sealed class GameSetProjectTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_editor_" + Guid.NewGuid().ToString("N"));

    public GameSetProjectTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Projectiles"));
        File.WriteAllText(Path.Combine(_root, "blocks.json"), "[ { \"ID\": \"stone\" }, { \"ID\": \"iron\" } ]");
        File.WriteAllText(Path.Combine(_root, "Projectiles", "p_a.json"), "{ \"Speed\": 10 }");
        File.WriteAllText(Path.Combine(_root, "recipes.json"), "{ \"RecipeCatalog\": [ { \"Type\": \"furnace\", \"Recipes\": [ {}, {} ] } ] }");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [Fact]
    public void ArraySource_AddDuplicateRemove()
    {
        var project = new GameSetProject(_root, "test");
        var source = new ArrayEntrySource(project, "blocks.json", "", GameSetCatalog.Blocks);

        var added = source.Add(null);
        Assert.Equal(3, source.GetEntries().Count);
        Assert.Equal("new_block", GameSetCatalog.Blocks.GetId(added.Node));

        var copy = source.Duplicate(source.GetEntries()[0]);
        Assert.Equal("stone_2", GameSetCatalog.Blocks.GetId(copy.Node));
        Assert.Same(copy.Node, source.GetEntries()[1].Node);

        source.Remove(copy);
        Assert.Equal(3, source.GetEntries().Count);
        Assert.Equal(1, project.DirtyCount);
    }

    [Fact]
    public void SaveAll_WritesDirtyFiles()
    {
        var project = new GameSetProject(_root, "test");
        var source = new ArrayEntrySource(project, "blocks.json", "", GameSetCatalog.Blocks);
        source.Add(null);

        project.SaveAll();

        var saved = EditorJsonFile.Parse(File.ReadAllText(Path.Combine(_root, "blocks.json")));
        Assert.Equal(3, saved!.AsArray().Count);
        Assert.Equal(0, project.DirtyCount);
    }

    [Fact]
    public void FolderSource_DeleteIsDeferredUntilSave()
    {
        var project = new GameSetProject(_root, "test");
        var source = new FolderEntrySource(project, "Projectiles", GameSetCatalog.Projectiles);
        var entry = source.GetEntries()[0];

        source.Remove(entry);

        Assert.Empty(source.GetEntries());
        Assert.True(File.Exists(Path.Combine(_root, "Projectiles", "p_a.json")));

        project.SaveAll();
        Assert.False(File.Exists(Path.Combine(_root, "Projectiles", "p_a.json")));
    }

    [Fact]
    public void FolderSource_AddCreatesFileOnSave()
    {
        var project = new GameSetProject(_root, "test");
        var source = new FolderEntrySource(project, "Projectiles", GameSetCatalog.Projectiles);

        var entry = source.Add(null);

        Assert.Equal(2, source.GetEntries().Count);
        Assert.Equal("Projectiles/p_new.json", entry.File.RelativePath);
        project.SaveAll();
        Assert.True(File.Exists(Path.Combine(_root, "Projectiles", "p_new.json")));
    }

    [Fact]
    public void DiscardChanges_RestoresFiles()
    {
        var project = new GameSetProject(_root, "test");
        var source = new FolderEntrySource(project, "Projectiles", GameSetCatalog.Projectiles);
        source.Remove(source.GetEntries()[0]);
        source.Add(null);

        project.DiscardChanges();

        var entries = source.GetEntries();
        Assert.Single(entries);
        Assert.Equal("Projectiles/p_a.json", entries[0].File.RelativePath);
        Assert.Equal(0, project.DirtyCount);
    }

    [Fact]
    public void GroupedSource_ListsGroupsAndAddsToNewGroup()
    {
        var project = new GameSetProject(_root, "test");
        var source = new GroupedArrayEntrySource(project, "recipes.json", "RecipeCatalog", "Type", "Recipes", GameSetCatalog.ProcessingRecipes);

        Assert.Equal(new[] { "furnace" }, source.Groups);
        Assert.Equal(2, source.GetEntries().Count);

        var entry = source.Add("crusher");

        Assert.Equal("crusher", entry.Group);
        Assert.Equal(new[] { "furnace", "crusher" }, source.Groups);
    }

    [Fact]
    public void ListJsonFiles_IncludesNewUnsavedFiles()
    {
        var project = new GameSetProject(_root, "test");
        project.CreateFile("Optional/new.json", new JsonObject());

        var files = project.ListJsonFiles();

        Assert.Contains("Optional/new.json", files);
        Assert.Contains("blocks.json", files);
    }
}
