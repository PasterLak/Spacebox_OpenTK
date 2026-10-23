using Spacebox.Game.Resource.Content;
using Spacebox.Network;

namespace Spacebox.Tests.GameSets;

public sealed class GameSetFallbacksTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    private GameSetStack Resolve(string id, params string[] extensions)
    {
        var result = GameSetStackResolver.Resolve(ModFolders.Flat(_sets.Root), id, extensions);
        Assert.True(result.Success, result.ErrorText);
        return result.Stack;
    }

    private void WriteBaseSets()
    {
        _sets.Config("Empty", "empty");
        _sets.Config("Default", "default", "empty");
    }

    private string[] Fallbacks(GameSetStack stack) => stack.Fallbacks.Select(f => f.Id).ToArray();

    [Theory]
    [InlineData("empty", false, true)]
    [InlineData("EMPTY", false, true)]
    [InlineData("default", true, false)]
    [InlineData("", false, false)]
    public void Roles_TreatGameSetsBuiltOnEmptyAsGameSets(string extends, bool isExtension, bool basedOnEmpty)
    {
        Assert.Equal(isExtension, GameSetRoles.IsExtension(extends));
        Assert.Equal(basedOnEmpty, GameSetRoles.IsBasedOnEmpty(extends));
    }

    [Fact]
    public void GameSetsBuiltOnEmpty_FallBackOnlyToEmpty()
    {
        WriteBaseSets();
        _sets.Config("Mine", "mine", "empty");

        Assert.Equal(new[] { "empty" }, Fallbacks(Resolve("default")));
        Assert.Equal(new[] { "empty" }, Fallbacks(Resolve("mine")));
        Assert.Empty(Fallbacks(Resolve("empty")));
    }

    [Fact]
    public void OldGameSetsWithoutExtends_FallBackToDefaultThenEmpty()
    {
        WriteBaseSets();
        _sets.Config("Old", "old");

        var stack = Resolve("old");

        Assert.Equal(new[] { "default", "empty" }, Fallbacks(stack));
        Assert.Equal("default", stack.Fallback.Id);
        Assert.Equal(new[] { "empty", "default", "old" }, stack.AssetLayers.Select(l => l.Id).ToArray());
    }

    [Fact]
    public void GameSetsBuiltOnEmpty_AreNotExtensionsAndCannotBeAddedAsOne()
    {
        WriteBaseSets();
        _sets.Config("Mine", "mine", "empty");
        var installed = InstalledGameSets.Scan(_sets.Root);

        Assert.DoesNotContain(InstalledGameSets.Find(installed, "mine"), InstalledGameSets.ExtensionsFor(installed, InstalledGameSets.Find(installed, "empty")));
        var result = GameSetStackResolver.Resolve(ModFolders.Flat(_sets.Root), "default", new[] { "mine" });
        Assert.False(result.Success);
        Assert.Contains("built on Empty", result.ErrorText);
    }

    [Fact]
    public void ContentOfGameSetsBuiltOnEmpty_ComesOnlyFromEmpty()
    {
        WriteBaseSets();
        _sets.Write("Empty/blocks.json", "[]");
        _sets.Write("Empty/items.json", "[]");
        _sets.Write("Default/Blocks/stone/block.json", "{ \"ID\": \"stone\" }");
        _sets.Config("Mine", "mine", "empty");

        Assert.Empty(Resolve("mine").ReadContent().Blocks);
        Assert.Single(Resolve("default").ReadContent().Blocks);
    }

    [Fact]
    public void ServerPackage_CarriesEmptySoClientsCanLoadGameSetsBuiltOnIt()
    {
        WriteBaseSets();
        _sets.Config("Mine", "mine", "empty");
        _sets.Write("Empty/Sounds/click.ogg", "x");

        var stack = Resolve("mine");
        string target = Path.Combine(_sets.Root, "Download");
        Engine.Utils.DirectoryArchive.Extract(GameSetFolders.Pack(stack), target);

        var downloaded = GameSetStackResolver.Resolve(new ModFolders(target), "mine", null);
        Assert.True(downloaded.Success, downloaded.ErrorText);
        Assert.Equal(new[] { "empty" }, Fallbacks(downloaded.Stack));
        Assert.True(File.Exists(Path.Combine(target, ModFolders.GameSetsName, "Empty", "Sounds", "click.ogg")));
        Assert.Equal(GameSetFolders.ComputeHash(stack), GameSetFolders.ComputeHash(downloaded.Stack));
    }
}
