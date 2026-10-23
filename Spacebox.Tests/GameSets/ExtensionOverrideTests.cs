using System.Text.Json.Nodes;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.GameSets;

public sealed class ExtensionOverrideTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    private GameSetStack Resolve(string baseId, params string[] extensions)
    {
        var result = GameSetStackResolver.Resolve(ModFolders.Flat(_sets.Root), baseId, extensions);
        Assert.True(result.Success, result.ErrorText);
        return result.Stack;
    }

    private void WriteBaseSets()
    {
        _sets.Config("Empty", "empty");
        _sets.Write("Empty/ships.json", "{ \"MaxSpeed\": 50, \"MaxAngularSpeed\": 2 }");
        _sets.Write("Empty/Optional/lighting.json", "{ \"FogDensity\": 1 }");
        _sets.Config("Default", "default", "empty");
        _sets.Write("Default/Projectiles/bullet.json", "{ \"Damage\": 10, \"Speed\": 100 }");
        _sets.Write("Default/Projectiles/laser.json", "{ \"Damage\": 5 }");
        _sets.Write("Default/Generation/generator.json", "{ \"MinAsteroidsInSector\": 10, \"Biomes\": [ \"field\" ] }");
        _sets.Write("Default/disassembly.json", "{ \"DefaultReturnPercent\": 50, \"Items\": [ { \"Item\": \"pistol\", \"ReturnPercent\": 40 }, { \"Item\": \"rifle\" } ] }");
        _sets.Write("Default/Blocks/stone/stone.json", "{ \"ID\": \"stone\", \"Name\": \"Stone\" }");
        _sets.Write("Default/Blocks/glass/glass.json", "{ \"ID\": \"glass\" }");
    }

    private void WritePack()
    {
        _sets.Config("Pack", "pack", "default");
        _sets.Write("Pack/Projectiles/bullet.json", "{ \"Damage\": 20 }");
        _sets.Write("Pack/Projectiles/laser.json", "{ \"Remove\": true }");
        _sets.Write("Pack/Projectiles/plasma.json", "{ \"Damage\": 30 }");
        _sets.Write("Pack/ships.json", "{ \"MaxSpeed\": 80 }");
        _sets.Write("Pack/Optional/lighting.json", "{ \"FogDensity\": 3 }");
        _sets.Write("Pack/Generation/generator.json", "{ \"MinAsteroidsInSector\": 20 }");
        _sets.Write("Pack/disassembly.json", "{ \"Items\": [ { \"Item\": \"pistol\", \"ReturnPercent\": 80 }, { \"Item\": \"rifle\", \"Remove\": true }, { \"Item\": \"plasma_gun\" } ] }");
        _sets.Write("Pack/Blocks/glass/glass.json", "{ \"ID\": \"glass\", \"Remove\": true }");
    }

    [Fact]
    public void FolderEntries_ArePatchedAddedAndRemovedByExtensions()
    {
        WriteBaseSets();
        WritePack();

        var projectiles = GameSetFiles.For(Resolve("default", "pack")).Folder("Projectiles");

        Assert.Equal(new[] { "bullet", "pack:plasma" }, projectiles.Select(p => p.Key).ToArray());
        Assert.Equal("pack:plasma", projectiles[1].Name);
        var bullet = (JsonObject)projectiles[0].Data;
        Assert.Equal(20, bullet["Damage"]!.GetValue<int>());
        Assert.Equal(100, bullet["Speed"]!.GetValue<int>());
    }

    [Fact]
    public void SettingsFiles_FromEmptyDefaultAndExtensionsAreMergedFieldByField()
    {
        WriteBaseSets();
        WritePack();
        var files = GameSetFiles.For(Resolve("default", "pack"));

        var ships = (JsonObject)files.File("ships.json")!.Data;
        var generator = (JsonObject)files.File("Generation/generator.json")!.Data;

        Assert.Equal(80, ships["MaxSpeed"]!.GetValue<int>());
        Assert.Equal(2, ships["MaxAngularSpeed"]!.GetValue<int>());
        Assert.Equal(3, files.File("Optional/lighting.json")!.Data!["FogDensity"]!.GetValue<int>());
        Assert.Equal(20, generator["MinAsteroidsInSector"]!.GetValue<int>());
        Assert.Equal("field", generator["Biomes"]![0]!.GetValue<string>());
    }

    [Fact]
    public void KeyedArrays_MergeEntriesById()
    {
        WriteBaseSets();
        WritePack();

        var items = (JsonArray)GameSetFiles.For(Resolve("default", "pack")).File("disassembly.json", new Dictionary<string, string> { ["Items"] = "Item" })!.Data!["Items"]!;

        Assert.Equal(new[] { "pistol", "plasma_gun" }, items.Select(i => i!["Item"]!.GetValue<string>()).ToArray());
        Assert.Equal(80, items[0]!["ReturnPercent"]!.GetValue<int>());
    }

    [Fact]
    public void Blocks_CanBeRemovedByAnExtension()
    {
        WriteBaseSets();
        WritePack();

        var content = Resolve("default", "pack").ReadContent();

        Assert.Equal(new[] { "stone" }, content.Blocks.Select(b => b.Key).ToArray());
        Assert.Empty(content.Warnings);
    }

    [Fact]
    public void AdapterFolders_LoadOnlyForTheirBase()
    {
        WriteBaseSets();
        _sets.Config("Mine", "mine", "empty");
        _sets.Write("Weapons/config.json", "{ \"ModId\": \"weapons\", \"Extends\": \"*\" }");
        _sets.Write("Weapons/Projectiles/plasma.json", "{ \"Damage\": 30 }");
        _sets.Write("Weapons/Bases/default/Projectiles/plasma.json", "{ \"Damage\": 99 }");
        _sets.Write("Weapons/Bases/default/Blocks/forge/forge.json", "{ \"ID\": \"weapons_forge\" }");

        var withDefault = Resolve("default", "weapons");
        var withMine = Resolve("mine", "weapons");

        Assert.Equal(99, GameSetFiles.For(withDefault).Folder("Projectiles").Single(p => p.Key == "weapons:plasma").Data!["Damage"]!.GetValue<int>());
        Assert.Equal(30, GameSetFiles.For(withMine).Folder("Projectiles").Single(p => p.Key == "weapons:plasma").Data!["Damage"]!.GetValue<int>());
        Assert.Contains(withDefault.ReadContent().Blocks, b => b.Key == "weapons:weapons_forge");
        Assert.DoesNotContain(withMine.ReadContent().Blocks, b => b.Key.EndsWith("weapons_forge"));
        Assert.Empty(withDefault.ReadContent().Warnings);
    }

    [Fact]
    public void BaseFolder_ReplacesTheFallbackFolderButExtensionsAddToIt()
    {
        WriteBaseSets();
        _sets.Write("Empty/Projectiles/empty_shot.json", "{ }");

        var files = GameSetFiles.For(Resolve("default"));

        Assert.Equal(new[] { "bullet", "laser" }, files.Folder("Projectiles").Select(p => p.Key).ToArray());
    }
}
