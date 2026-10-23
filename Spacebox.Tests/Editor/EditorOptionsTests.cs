using Spacebox.Editor.Canvas.Inspector;
using Spacebox.Editor.Data;
using Spacebox.Game.Generation.Shapes;
using Spacebox.Game.Mobs;
using Spacebox.Game.Tutorial;
using Spacebox.Game.Tutorial.Tasks;

namespace Spacebox.Tests.Editor;

public sealed class EditorOptionsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_options_" + Guid.NewGuid().ToString("N"));

    private static FieldHelp Help()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Spacebox", FieldHelp.RelativePath);
            if (File.Exists(candidate)) return FieldHelp.Load(candidate);
            dir = dir.Parent;
        }
        return FieldHelp.Load(Path.Combine(AppContext.BaseDirectory, FieldHelp.RelativePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [Fact]
    public void EveryTypeInADropDownHasADescription()
    {
        var help = Help();
        var missing = new List<string>();
        foreach (var type in GameSetCatalog.BlockTypeNames) if (help.Text(FieldHelp.BlockTypes, type) == null) missing.Add("block " + type);
        foreach (var name in Spacebox.Game.Resource.BlockComponentRegistry.Names) if (help.Text(FieldHelp.BlockComponents, name) == null) missing.Add("component " + name);
        foreach (var type in GameSetCatalog.ItemTypeNames) if (help.Text(FieldHelp.ItemTypes, type) == null) missing.Add("item " + type);
        foreach (var type in MobBrainFactory.Types) if (help.Text(FieldHelp.MobBehaviours, type) == null) missing.Add("mob " + type);
        foreach (var type in TutorialTaskFactory.Types) if (!type.StartsWith("test", StringComparison.OrdinalIgnoreCase) && help.Text(FieldHelp.TutorialTasks, type) == null) missing.Add("task " + type);

        Assert.True(missing.Count == 0, "Missing descriptions: " + string.Join(", ", missing));
    }

    [Fact]
    public void ProjectFiles_AreListedRelativeWithForwardSlashes()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Models", "Armor"));
        Directory.CreateDirectory(Path.Combine(_root, "Models", "Blocks"));
        File.WriteAllText(Path.Combine(_root, "Models", "Armor", "helmet.obj"), "");
        File.WriteAllText(Path.Combine(_root, "Models", "Armor", "iron.PNG"), "");
        File.WriteAllText(Path.Combine(_root, "Models", "Blocks", "torch.obj"), "");

        Assert.Equal(new[] { "Models/Armor/helmet.obj", "Models/Blocks/torch.obj" }, ProjectFiles.List(_root, "", ".obj"));
        Assert.Equal(new[] { "Models/Armor/iron.PNG" }, ProjectFiles.List(_root, "Models", EditorOptions.ImageExtensions));
        Assert.Equal(new[] { "torch" }, ProjectFiles.Names(_root, "Models/Blocks", ".obj"));
        Assert.Empty(ProjectFiles.List(_root, "Missing", ".obj"));
    }

    [Fact]
    public void FixedOptions_MatchWhatTheGameReads()
    {
        Assert.Contains("Up", EditorOptions.Directions);
        Assert.Contains("Forward", EditorOptions.Directions);
        Assert.Contains("DistanceTraveled", PlayerStatReader.Names);
        Assert.All(PlayerStatReader.Names, name => Assert.True(PlayerStatReader.IsKnown(name)));
        Assert.Contains("slab", BlockGeometryLibrary.Names);
        Assert.Contains("inventory", TutorialWindows.Known);
        Assert.Contains("Survival", EditorOptions.GameModeNames);
    }
}
