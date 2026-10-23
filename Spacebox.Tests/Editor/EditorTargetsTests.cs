using Spacebox.Editor;
using Spacebox.Game.Resource.Content;
using Spacebox.Tests.GameSets;

namespace Spacebox.Tests.Editor;

public sealed class EditorTargetsTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    [Fact]
    public void Packs_CanTargetEveryGameSetTheyWorkWith()
    {
        _sets.Config("Empty", "empty");
        _sets.Config("Default", "default", "empty");
        _sets.Config("Mine", "mine", "empty");
        _sets.Config("Weapons", "weapons", "*");
        _sets.Config("Lamps", "lamps", "default");
        var installed = InstalledGameSets.Scan(_sets.Root);

        var weapons = EditorTargets.For(InstalledGameSets.Find(installed, "weapons"), installed);
        var lamps = EditorTargets.For(InstalledGameSets.Find(installed, "lamps"), installed);

        Assert.Equal(new[] { "", "default", "mine" }, weapons.Select(t => t.Id).ToArray());
        Assert.Equal(new[] { "", "default" }, lamps.Select(t => t.Id).ToArray());
        Assert.Empty(EditorTargets.For(InstalledGameSets.Find(installed, "default"), installed));
    }

    [Fact]
    public void TargetedEdits_GoIntoTheAdapterFolder()
    {
        string pack = _sets.Folder("Weapons");

        Assert.Equal(pack, EditorTargets.ProjectRoot(pack, ""));
        string root = EditorTargets.ProjectRoot(pack, "default");
        Assert.Equal(Path.Combine(pack, "Bases", "default"), root);
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public void TargetFolder_IsCreatedOnFirstSave()
    {
        string pack = _sets.Folder("Weapons");
        string root = EditorTargets.ProjectRoot(pack, "default");
        var project = new Spacebox.Editor.Data.GameSetProject(root, "weapons");

        Assert.Empty(project.ListJsonFiles());
        project.CreateFile("blocks.json", new System.Text.Json.Nodes.JsonArray());
        project.SaveAll();

        Assert.True(File.Exists(Path.Combine(root, "blocks.json")));
    }
}
