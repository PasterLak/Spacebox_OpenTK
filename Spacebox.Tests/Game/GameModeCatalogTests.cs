using System.Text.Json;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Player.Interactions;

namespace Spacebox.Tests.Game;

[Collection("GameModeCatalog")]
public sealed class GameModeCatalogTests : IDisposable
{
    public void Dispose() => GameModeCatalog.Reset();

    private static GameModeDefinition Parse(string json)
    {
        Assert.True(GameModeParser.TryConvert(JsonSerializer.Deserialize<GameModeJSON>(json), out var mode, out string error), error);
        return mode;
    }

    [Fact]
    public void CustomMode_InheritsBaseRulesAndOverridesSome()
    {
        var mode = Parse("""{ "Id": 3, "Key": "explorer", "Name": "Explorer", "Base": "survival", "Rules": { "BreakBlocks": false } }""");

        Assert.Equal(GameMode.Survival, mode.Base);
        Assert.False(mode.Rules.BreakBlocks);
        Assert.True(mode.Rules.PlaceBlocks);
        Assert.True(mode.Rules.TakeDamage);
    }

    [Fact]
    public void BuiltInIds_KeepTheirBase()
    {
        var mode = Parse("""{ "Id": 1, "Base": "spectator", "Rules": { "TakeDamage": true } }""");

        Assert.Equal(GameMode.Creative, mode.Base);
        Assert.True(mode.Rules.TakeDamage);
        Assert.True(mode.Rules.BreakBlocks);
    }

    [Theory]
    [InlineData("""{ "Id": -1 }""")]
    [InlineData("""{ "Id": 40 }""")]
    [InlineData("""{ "Id": 5, "Base": "flying" }""")]
    public void InvalidModes_AreRejected(string json)
    {
        Assert.False(GameModeParser.TryConvert(JsonSerializer.Deserialize<GameModeJSON>(json), out _, out _));
    }

    [Fact]
    public void Catalog_FindsByIdKeyOrNameAndFallsBackToSurvival()
    {
        GameModeCatalog.Set(new[] { Parse("""{ "Id": 3, "Key": "explorer", "Name": "Explorer", "Base": "creative" }""") });

        Assert.True(GameModeCatalog.TryFind("3", out var byId));
        Assert.True(GameModeCatalog.TryFind("EXPLORER", out var byKey));
        Assert.True(GameModeCatalog.TryFind("creative", out var builtIn));
        Assert.Equal(byId, byKey);
        Assert.Equal(GameMode.Creative, builtIn.Id);
        Assert.Equal(GameMode.Creative, GameModeCatalog.BaseOf((GameMode)3));
        Assert.Equal(GameMode.Survival, GameModeCatalog.BaseOf((GameMode)9));
        Assert.False(GameModeCatalog.TryFind("9", out _));
        Assert.Equal(4, GameModeCatalog.All.Count);
    }

    [Fact]
    public void Interactions_AreRemovedByRules()
    {
        var allowed = new HashSet<Type> { typeof(InteractionDestroyBlockSurvival), typeof(InteractionPlaceBlock), typeof(InteractionShoot), typeof(InteractionEraser), typeof(InteractionDefault) };

        GameModeInteractions.Filter(allowed, new GameModeRules { BreakBlocks = false, UseWeapons = false });

        Assert.Equal(new HashSet<Type> { typeof(InteractionPlaceBlock), typeof(InteractionDefault) }, allowed);
    }

    [Fact]
    public void DefaultGameSet_DefinesTheBuiltInModesAndExplorer()
    {
        string folder = Path.Combine(DefaultGameSetContent.Folder, "GameModes");
        var modes = Directory.GetFiles(folder, "*.json").Select(file => Parse(File.ReadAllText(file))).ToList();

        Assert.Contains(modes, m => m.Id == GameMode.Survival && m.Rules.NeedOxygen);
        Assert.Contains(modes, m => m.Id == GameMode.Creative && !m.Rules.TakeDamage);
        Assert.Contains(modes, m => m.Id == GameMode.Spectator && !m.Rules.BreakBlocks);
        Assert.Contains(modes, m => m.Key == "explorer" && m.Base == GameMode.Survival && !m.Rules.PlaceBlocks);
    }
}
