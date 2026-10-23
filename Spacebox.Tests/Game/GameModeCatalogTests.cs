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

    [Fact]
    public void Tag_ComesFromJsonOrDefaultsToFirstLetterAndBaseColor()
    {
        var tagged = Parse("""{ "Id": 3, "Key": "explorer", "Name": "Explorer", "Base": "survival", "Tag": "Expl", "TagColor": "#C08A2E" }""");
        Assert.Equal("Exp", tagged.Tag);
        Assert.Equal("#C08A2E", tagged.TagColor);

        var plain = Parse("""{ "Id": 4, "Key": "builder", "Name": "builder", "Base": "creative" }""");
        Assert.Equal("B", plain.Tag);
        Assert.Equal(GameModeTags.DefaultColor(GameMode.Creative), plain.TagColor);
    }

    [Fact]
    public void NewRules_KeepTheBehaviourOfTheThreeBases()
    {
        var survival = GameModeRules.ForBase(GameMode.Survival);
        var creative = GameModeRules.ForBase(GameMode.Creative);
        var spectator = GameModeRules.ForBase(GameMode.Spectator);

        Assert.True(survival.MobsAttack && survival.ConsumeItems && survival.DropItemsOnDeath && survival.Collide && survival.UseGravity);
        Assert.False(survival.CreativeInventory || survival.InstantBreak || survival.ShipLibrary || survival.FullRadar);
        Assert.True(creative.CreativeInventory && creative.InstantBreak && creative.BreakUnbreakable && creative.AreaTools && creative.ShipLibrary);
        Assert.False(creative.MobsAttack || creative.ConsumeItems || creative.DropItemsOnDeath);
        Assert.False(spectator.Collide || spectator.UseGravity || spectator.UseItems || spectator.CountStatistics || spectator.StartItems);
        Assert.True(spectator.FreeFlashlight && spectator.FullRadar);
    }

    [Fact]
    public void NewRules_CanBeSetPerMode()
    {
        var mode = Parse("""{ "Id": 5, "Key": "ghost", "Name": "Ghost", "Base": "survival", "Rules": { "Collide": false, "MobsAttack": false, "UseGravity": false, "FullRadar": true, "InstantBreak": true } }""");

        Assert.False(mode.Rules.Collide);
        Assert.False(mode.Rules.MobsAttack);
        Assert.False(mode.Rules.UseGravity);
        Assert.True(mode.Rules.FullRadar);
        Assert.True(mode.Rules.InstantBreak);
        Assert.True(mode.Rules.ConsumeItems);
    }

    [Fact]
    public void InstantBreak_PicksTheMatchingDrillInteraction()
    {
        var survivalSet = new HashSet<Type> { typeof(InteractionDestroyBlockSurvival), typeof(InteractionPlaceBlock) };
        var creativeSet = new HashSet<Type> { typeof(InteractionDestroyBlockCreative), typeof(InteractionPlaceBlock) };

        var instant = GameModeInteractions.Filter(survivalSet, new GameModeRules { InstantBreak = true });
        var drilling = GameModeInteractions.Filter(creativeSet, new GameModeRules { InstantBreak = false });

        Assert.Contains(typeof(InteractionDestroyBlockCreative), instant);
        Assert.DoesNotContain(typeof(InteractionDestroyBlockSurvival), instant);
        Assert.Contains(typeof(InteractionDestroyBlockSurvival), drilling);
        Assert.DoesNotContain(typeof(InteractionDestroyBlockCreative), drilling);
    }
}
