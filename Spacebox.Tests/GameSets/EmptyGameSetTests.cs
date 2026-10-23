using System.Text.Json;
using Spacebox.Game.Resource;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.GameSets;

public sealed class EmptyGameSetTests
{
    private static ModFolders Mods => new ModFolders(Path.GetDirectoryName(Path.GetDirectoryName(DefaultGameSetContent.Folder))!);

    private static string Folder => Path.Combine(Path.GetDirectoryName(DefaultGameSetContent.Folder)!, "Empty");

    [Fact]
    public void EmptyGameSet_TakesNoContentFromDefault()
    {
        var result = GameSetStackResolver.Resolve(Mods, "empty", Array.Empty<string>());
        Assert.True(result.Success, result.ErrorText);

        var content = result.Stack.ReadContent();

        Assert.Empty(content.Warnings);
        Assert.Empty(content.Blocks);
        Assert.Empty(content.Items);
        Assert.Empty(content.Blueprints);
        Assert.Empty(content.Categories);
        Assert.Empty(content.Recipes);
    }

    [Fact]
    public void EmptyGameSet_HasOwnFoldersSoNoFolderFallsBackToDefault()
    {
        foreach (var folder in new[] { "Projectiles", "Mobs", "Tutorials", GameModesLoader.FolderName, "Generation/Asteroids", "Generation/Biomes", "Generation/Loot" })
            Assert.True(Directory.Exists(Path.Combine(Folder, folder)), folder);

        Assert.Empty(Directory.GetFiles(Path.Combine(Folder, "Generation", "Asteroids"), "*.json"));
        Assert.Equal(3, GameModesLoader.ReadFolder(Path.Combine(Folder, GameModesLoader.FolderName)).Count);
    }

    [Fact]
    public void EmptyGameSet_GeneratesNoAsteroids()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Folder, "Generation", "generator.json")));
        Assert.Equal(0, document.RootElement.GetProperty("MaxAsteroidsInSector").GetInt32());
        Assert.Equal(0, document.RootElement.GetProperty("Biomes").GetArrayLength());
    }
}
