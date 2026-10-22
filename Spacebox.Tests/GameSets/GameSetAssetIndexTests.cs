using Spacebox.Game;
using Spacebox.Game.Resource.Content;
using Spacebox.Game.Resource.TexturePacks;

namespace Spacebox.Tests.GameSets;

public sealed class GameSetAssetIndexTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    private ContentLayer Layer(string name) => new ContentLayer(name.ToLowerInvariant(), Path.Combine(_sets.Root, name));

    [Fact]
    public void LaterLayersOverrideTexturesAndSoundsByName()
    {
        _sets.Write("Base/Blocks/Rocks/stone.png", "a");
        _sets.Write("Base/Blocks/iron.png", "a");
        _sets.Write("Base/Items/ingot.png", "a");
        _sets.Write("Base/Sounds/place.ogg", "a");
        _sets.Write("Ext/Blocks/stone/stone.png", "b");
        _sets.Write("Ext/Blocks/lamp/lamp.png", "b");
        _sets.Write("Ext/Blocks/lamp/click.ogg", "b");
        _sets.Write("Ext/Sounds/place.ogg", "b");

        var index = GameSetAssetIndex.Build(new[] { Layer("Base"), Layer("Ext") });

        Assert.Contains(Path.Combine("Ext", "Blocks", "stone", "stone.png"), index.BlockTextures["stone"]);
        Assert.Contains(Path.Combine("Base", "Blocks", "iron.png"), index.BlockTextures["iron"]);
        Assert.True(index.BlockTextures.ContainsKey("lamp"));
        Assert.True(index.ItemTextures.ContainsKey("ingot"));
        Assert.Contains(Path.Combine("Ext", "Sounds"), index.Sounds["place"]);
        Assert.True(index.Sounds.ContainsKey("click"));
    }

    [Fact]
    public void EntryEmissionsStayInTheirAtlasAndSharedEmissionsGoToBoth()
    {
        _sets.Write("Base/Blocks/furnace/furnace.png", "a");
        _sets.Write("Base/Blocks/furnace/Emissions/furnace.png", "a");
        _sets.Write("Base/Items/torch/torch.png", "a");
        _sets.Write("Base/Emissions/glow.png", "a");

        var index = GameSetAssetIndex.Build(new[] { Layer("Base") });

        Assert.Contains("Emissions", index.BlockEmissions["furnace"]);
        Assert.DoesNotContain("Emissions", index.BlockTextures["furnace"]);
        Assert.False(index.ItemEmissions.ContainsKey("furnace"));
        Assert.True(index.BlockEmissions.ContainsKey("glow"));
        Assert.True(index.ItemEmissions.ContainsKey("glow"));
    }

    [Fact]
    public void IgnoredFoldersAreSkippedAndFirstSortedFileWinsInsideALayer()
    {
        _sets.Write("Base/Blocks/_Ignored/stone.png", "a");
        _sets.Write("Base/Blocks/b/stone.png", "a");
        _sets.Write("Base/Blocks/a/stone.png", "a");

        var index = GameSetAssetIndex.Build(new[] { Layer("Base") });

        Assert.Contains(Path.Combine("Blocks", "a", "stone.png"), index.BlockTextures["stone"]);
    }

    [Fact]
    public void BuiltInTexturesOnlyFillMissingNames()
    {
        _sets.Write("Base/Blocks/lod.png", "a");
        _sets.Write("BuiltIn/lod.png", "b");
        _sets.Write("BuiltIn/sand.png", "b");

        var index = GameSetAssetIndex.Build(new[] { Layer("Base") }, Path.Combine(_sets.Root, "BuiltIn"));

        Assert.Contains("Base", index.BlockTextures["lod"]);
        Assert.Contains("BuiltIn", index.BlockTextures["sand"]);
    }

    [Fact]
    public void TexturePacks_FirstEnabledPackWinsAndOnlyExistingNamesAreReplaced()
    {
        _sets.Write("Base/Blocks/stone.png", "a");
        _sets.Write("Base/Blocks/iron.png", "a");
        _sets.Write("Base/Items/ingot.png", "a");
        _sets.Write("Packs/Top/Blocks/stone.png", "top");
        _sets.Write("Packs/Top/Blocks/unknown.png", "top");
        _sets.Write("Packs/Low/Blocks/stone.png", "low");
        _sets.Write("Packs/Low/Blocks/iron.png", "low");
        _sets.Write("Packs/Low/Items/sub/ingot.png", "low");
        _sets.Write("Packs/Low/Emissions/iron.png", "low");

        var packs = TexturePackCatalog.Scan(Path.Combine(_sets.Root, "Packs"));
        var active = TexturePackCatalog.Active(packs, new[] { "top", "Low" });
        var index = GameSetAssetIndex.Build(new[] { Layer("Base") }, null, null, active.Cast<ITextureOverrideSource>().ToList());

        Assert.Contains(Path.Combine("Top", "Blocks", "stone.png"), index.BlockTextures["stone"]);
        Assert.Contains(Path.Combine("Low", "Blocks", "iron.png"), index.BlockTextures["iron"]);
        Assert.Contains(Path.Combine("Low", "Items"), index.ItemTextures["ingot"]);
        Assert.Contains("Low", index.BlockEmissions["iron"]);
        Assert.False(index.ItemEmissions.ContainsKey("iron"));
        Assert.False(index.BlockTextures.ContainsKey("unknown"));
    }

    [Fact]
    public void TexturePacks_ReadInfoAndUIOverrides()
    {
        _sets.Write("Packs/Shiny/pack.json", "{ \"Id\": \"shiny\", \"Name\": \"Shiny Pack\", \"Author\": \"me\" }");
        _sets.Write("Packs/Shiny/UI/Buttons/Slot.png", "x");

        var pack = Assert.Single(TexturePackCatalog.Scan(Path.Combine(_sets.Root, "Packs")));

        Assert.Equal("shiny", pack.Id);
        Assert.Equal("Shiny Pack", pack.DisplayName);
        Assert.True(pack.UI.ContainsKey("buttons/slot"));
        Assert.True(pack.Matches("Shiny"));
    }

    [Fact]
    public void TexturePackOrder_ToggleMovesAndKeepsAStack()
    {
        var enabled = new List<string>();

        TexturePackOrder.Toggle(enabled, "a");
        TexturePackOrder.Toggle(enabled, "b");
        Assert.Equal(new[] { "b", "a" }, enabled);

        Assert.True(TexturePackOrder.MoveDown(enabled, "b"));
        Assert.Equal(new[] { "a", "b" }, enabled);
        Assert.False(TexturePackOrder.MoveDown(enabled, "b"));
        Assert.True(TexturePackOrder.MoveUp(enabled, "B"));
        Assert.Equal(new[] { "b", "a" }, enabled);

        TexturePackOrder.Toggle(enabled, "b");
        Assert.Equal(new[] { "a" }, enabled);
    }

    [Fact]
    public void TexturePackSettings_ValidateRemovesBlanksAndDuplicates()
    {
        var settings = new TexturePackSettings { Enabled = new List<string> { "a", "", "A", "b", " " } };

        Assert.True(settings.Validate());
        Assert.Equal(new[] { "a", "b" }, settings.Enabled);
        Assert.False(settings.Validate());

        var nullList = new TexturePackSettings { Enabled = null! };
        Assert.True(nullList.Validate());
        Assert.Empty(nullList.Enabled);
    }
}
