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
    public void SaveAll_SavesTheOtherFilesAndReportsFailures()
    {
        Directory.CreateDirectory(Path.Combine(_root, "bad.json"));
        var project = new GameSetProject(_root, "test");
        project.CreateFile("bad.json", new JsonArray());
        project.CreateFile("good.json", new JsonArray());

        var error = Assert.ThrowsAny<IOException>(() => project.SaveAll());

        Assert.Contains("bad.json", error.Message);
        Assert.True(File.Exists(Path.Combine(_root, "good.json")));
        Assert.Equal(1, project.DirtyCount);
    }

    [Fact]
    public void ArraySource_DoesNotReplaceARootOfTheWrongKind()
    {
        File.WriteAllText(Path.Combine(_root, "items.json"), "{ \"ID\": \"keep\" }");
        var project = new GameSetProject(_root, "test");
        var source = new ArrayEntrySource(project, "items.json", "", GameSetCatalog.Items);

        Assert.Null(source.Add(null));
        Assert.Equal(0, project.DirtyCount);
        Assert.Equal("keep", project.GetFile("items.json").Root!["ID"]!.GetValue<string>());
    }

    [Fact]
    public void GroupedSource_DoesNotReplaceARootOfTheWrongKind()
    {
        File.WriteAllText(Path.Combine(_root, "recipes.json"), "[ 1 ]");
        var project = new GameSetProject(_root, "test");
        var source = new GroupedArrayEntrySource(project, "recipes.json", "RecipeCatalog", "Type", "Recipes", GameSetCatalog.ProcessingRecipes);

        Assert.Null(source.Add("furnace"));
        Assert.Equal(0, project.DirtyCount);
        Assert.IsType<JsonArray>(project.GetFile("recipes.json").Root);
    }

    [Fact]
    public void GroupedSource_RemovesAndDuplicatesInGroupsWithoutType()
    {
        File.WriteAllText(Path.Combine(_root, "recipes.json"), "{ \"RecipeCatalog\": [ { \"Recipes\": [ { \"A\": 1 } ] } ] }");
        var project = new GameSetProject(_root, "test");
        var source = new GroupedArrayEntrySource(project, "recipes.json", "RecipeCatalog", "Type", "Recipes", GameSetCatalog.ProcessingRecipes);
        var entry = Assert.Single(source.GetEntries());

        var copy = source.Duplicate(entry);
        Assert.Equal(new[] { "group0" }, source.Groups);
        Assert.Equal(2, source.GetEntries().Count);

        source.Remove(copy);
        source.Remove(source.GetEntries()[0]);

        Assert.Empty(source.GetEntries());
        Assert.Single(project.GetFile("recipes.json").Root!["RecipeCatalog"]!.AsArray());
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

    [Fact]
    public void BrokenFile_RefusesAddAndDuplicateAndIsNotSaved()
    {
        File.WriteAllText(Path.Combine(_root, "broken.json"), "{ \"Blueprints\": [ { ");
        string original = File.ReadAllText(Path.Combine(_root, "broken.json"));
        var project = new GameSetProject(_root, "test");
        var source = new ArrayEntrySource(project, "broken.json", "Blueprints", GameSetCatalog.Blueprints);
        var file = project.GetFile("broken.json");
        Assert.NotNull(file.Error);

        Assert.Null(source.Add(null));
        file.MarkDirty();
        project.SaveAll();

        Assert.Null(file.Root);
        Assert.Equal(original, File.ReadAllText(Path.Combine(_root, "broken.json")));
    }

    [Fact]
    public void BrokenGroupedFile_RefusesAdd()
    {
        File.WriteAllText(Path.Combine(_root, "recipes.json"), "{ \"RecipeCatalog\": [ ");
        var project = new GameSetProject(_root, "test");
        var source = new GroupedArrayEntrySource(project, "recipes.json", "RecipeCatalog", "Type", "Recipes", GameSetCatalog.ProcessingRecipes);

        Assert.Null(source.Add("furnace"));
        Assert.Null(project.GetFile("recipes.json").Root);
    }

    [Fact]
    public void ReplacingABrokenFile_ClearsTheErrorAndSaves()
    {
        File.WriteAllText(Path.Combine(_root, "broken.json"), "{ \"a\": [ ");
        var project = new GameSetProject(_root, "test");
        var file = project.GetFile("broken.json");

        file.Replace(new JsonObject { ["a"] = 1 });
        project.SaveAll();

        Assert.Null(file.Error);
        Assert.Equal(1, EditorJsonFile.Parse(File.ReadAllText(Path.Combine(_root, "broken.json")))!["a"]!.GetValue<int>());
    }

    [Fact]
    public void GetFile_TreatsPendingDeletesAsMissing()
    {
        var project = new GameSetProject(_root, "test");
        project.DeleteFile(project.GetFile("Projectiles/p_a.json"));

        var file = project.GetFile("Projectiles/p_a.json");

        Assert.Null(file.Root);
        Assert.False(file.IsDirty);
        Assert.False(project.Exists("Projectiles/p_a.json"));
        Assert.Equal(1, project.DirtyCount);
        Assert.DoesNotContain("Projectiles/p_a.json", project.ListJsonFiles());
    }

    [Fact]
    public void DiscardChanges_BringsBackFilesThatWereReadWhilePendingDelete()
    {
        var project = new GameSetProject(_root, "test");
        project.DeleteFile(project.GetFile("Projectiles/p_a.json"));
        project.GetFile("Projectiles/p_a.json");

        project.DiscardChanges();

        Assert.NotNull(project.GetFile("Projectiles/p_a.json").Root);
    }

    [Fact]
    public void ReloadFromDisk_ReloadsFileObjectsInPlace()
    {
        var project = new GameSetProject(_root, "test");
        var file = project.GetFile("Projectiles/p_a.json");
        int version = project.Version;
        File.WriteAllText(Path.Combine(_root, "Projectiles", "p_a.json"), "{ \"Speed\": 20 }");

        project.ReloadFromDisk();
        file.Root!["Speed"] = 30;
        file.MarkDirty();
        project.SaveAll();

        Assert.Same(file, project.GetFile("Projectiles/p_a.json"));
        Assert.True(project.Version > version);
        Assert.Equal(30, EditorJsonFile.Parse(File.ReadAllText(Path.Combine(_root, "Projectiles", "p_a.json")))!["Speed"]!.GetValue<int>());
    }

    [Fact]
    public void ReloadFromDisk_DropsFilesThatNoLongerExist()
    {
        var project = new GameSetProject(_root, "test");
        project.GetFile("blocks.json");
        File.Delete(Path.Combine(_root, "blocks.json"));

        project.ReloadFromDisk();

        Assert.DoesNotContain("blocks.json", project.ListJsonFiles());
        Assert.Equal(0, project.DirtyCount);
    }

    [Theory]
    [InlineData("Optional/new.json", true)]
    [InlineData("new.json", true)]
    [InlineData("C:/x.json", false)]
    [InlineData("C:x.json", false)]
    [InlineData("/etc/x.json", false)]
    [InlineData("\\\\server\\x.json", false)]
    [InlineData("../x.json", false)]
    [InlineData("a/../../x.json", false)]
    [InlineData("x.txt", false)]
    [InlineData(" ", false)]
    public void IsValidNewFileName_KeepsFilesInsideTheGameSet(string name, bool valid)
    {
        Assert.Equal(valid, GameSetProject.IsValidNewFileName(name));
    }

    [Fact]
    public void GameModes_AddAndDuplicatePickTheLowestFreeId()
    {
        Directory.CreateDirectory(Path.Combine(_root, "GameModes"));
        File.WriteAllText(Path.Combine(_root, "GameModes", "survival.json"), "{ \"Id\": 0, \"Key\": \"survival\" }");
        File.WriteAllText(Path.Combine(_root, "GameModes", "explorer.json"), "{ \"Id\": 3, \"Key\": \"explorer\" }");
        File.WriteAllText(Path.Combine(_root, "GameModes", "hard.json"), "{ \"Id\": 5, \"Key\": \"hard\" }");
        var project = new GameSetProject(_root, "test");
        var source = new FolderEntrySource(project, "GameModes", GameSetCatalog.GameModes);

        var added = source.Add(null);
        var copy = source.Duplicate(added);

        Assert.Equal(4, added.Node!["Id"]!.GetValue<int>());
        Assert.Equal(6, copy.Node!["Id"]!.GetValue<int>());
    }
}
