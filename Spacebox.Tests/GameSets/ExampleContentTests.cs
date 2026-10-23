using Spacebox.Game.Resource.Content;
using Spacebox.Game.Resource.TexturePacks;

namespace Spacebox.Tests.GameSets;

public sealed class ExampleContentTests
{
    private static ModFolders Mods => new ModFolders(Path.GetDirectoryName(Path.GetDirectoryName(DefaultGameSetContent.Folder))!);

    private static GameSetStack ExampleStack()
    {
        var result = GameSetStackResolver.Resolve(Mods, "default", new[] { "example_extension" });
        Assert.True(result.Success, result.ErrorText);
        return result.Stack;
    }

    [Fact]
    public void ExampleExtension_AddsContentOnTopOfDefault()
    {
        var content = ExampleStack().ReadContent();

        Assert.Empty(content.Warnings);
        Assert.Equal(DefaultGameSetContent.Content.Blocks.Count + 2, content.Blocks.Count);
        Assert.Equal("example_extension:example_striped_panel", content.Blocks[^1].Key);
        Assert.NotNull(content.FindItem("example_extension:example_glow_shard"));
        Assert.Contains(content.Recipes, r => r.Key == "crusher|example_extension:example_glow_block");
        Assert.Contains(content.Blueprints, b => b.Key == "example_extension:example_glow_block");
        Assert.Contains("patched", ContentJson.GetString(content.FindBlock("light_white").Data, "Description"));
        Assert.Equal("Crimson Wall Torch", ContentJson.GetString(content.FindBlock("light_red").Data, "Name"));
    }

    [Fact]
    public void ExampleExtension_ReferencesOnlyExistingIdsTexturesAndSounds()
    {
        var stack = ExampleStack();
        var content = stack.ReadContent();
        var assets = GameSetAssetIndex.Build(stack.Layers);
        var ids = new HashSet<string>(content.Blocks.Select(b => b.Key).Concat(content.Items.Select(i => i.Key)), StringComparer.OrdinalIgnoreCase);

        foreach (var entry in content.Blueprints.Concat(content.Recipes).Where(e => e.Layer.Id == "example_extension"))
        {
            foreach (var value in Spacebox.Game.Resource.Content.Conversion.TextureOwnership.Strings(entry.Data))
            {
                if (value.Contains('_')) Assert.Contains(value, ids);
            }
        }

        Assert.True(assets.BlockTextures.ContainsKey("exampleglowblock"));
        Assert.True(assets.BlockEmissions.ContainsKey("exampleglowblock"));
        Assert.True(assets.ItemTextures.ContainsKey("exampleglowshard"));
        Assert.True(assets.Sounds.ContainsKey("exampleglowplace"));
    }

    [Fact]
    public void ExampleExtension_IsListedForDefaultOnly()
    {
        var installed = InstalledGameSets.Scan(Mods);
        var extensions = InstalledGameSets.ExtensionsFor(installed, InstalledGameSets.Find(installed, "default"));

        Assert.Contains(extensions, e => e.Id == "example_extension");
        Assert.DoesNotContain(installed, g => g.Id == "default" && g.IsExtension);
    }

    [Fact]
    public void ExamplePack_OverridesExistingDefaultTextures()
    {
        var pack = Assert.Single(TexturePackCatalog.Scan(Mods.TexturePacks), p => p.Id == "example_pack");
        var assets = GameSetAssetIndex.Build(new[] { new ContentLayer("default", DefaultGameSetContent.Folder) }, null, null, new ITextureOverrideSource[] { pack });

        Assert.StartsWith(pack.Folder, assets.BlockTextures["lightrock"]);
        Assert.StartsWith(pack.Folder, assets.BlockTextures["mediumrock"]);
        Assert.StartsWith(pack.Folder, assets.ItemTextures["ironingot"]);
        Assert.StartsWith(DefaultGameSetContent.Folder, assets.BlockTextures["heavyrock"]);
    }
}
