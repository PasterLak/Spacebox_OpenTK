using System.Text.Json;
using Spacebox.Game.GUI;
using Spacebox.Game.Resource.Content;
using Spacebox.Game.Versioning;
using Spacebox.Network;

namespace Spacebox.Tests.GameSets;

public sealed class WorldExtensionsTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    [Fact]
    public void WorldInfo_SavesAndLoadsTheExtensionList()
    {
        var info = new WorldInfo { Name = "W", ModId = "default", Extensions = new List<string> { "lamps", "tools" } };

        var loaded = JsonSerializer.Deserialize<WorldInfo>(JsonSerializer.Serialize(info))!;

        Assert.Equal(new[] { "lamps", "tools" }, loaded.Extensions);
        Assert.Equal(new[] { "lamps", "tools" }, WorldGameSetCheck.ExtensionsOf(loaded));
    }

    [Fact]
    public void OldWorldJson_WithoutExtensions_LoadsWithNone()
    {
        var loaded = JsonSerializer.Deserialize<WorldInfo>("{ \"Name\": \"Old\", \"ModId\": \"default\", \"GameVersion\": \"0.1.5\" }")!;
        var broken = JsonSerializer.Deserialize<WorldInfo>("{ \"Name\": \"Old\", \"Extensions\": null }")!;

        Assert.Empty(loaded.Extensions);
        Assert.Empty(WorldGameSetCheck.ExtensionsOf(broken));
    }

    [Fact]
    public void ExtensionList_ParsesAndFormatsForTheNetwork()
    {
        Assert.Equal(new[] { "a", "b" }, WorldExtensionList.Parse(" a, b ,,A"));
        Assert.Empty(WorldExtensionList.Parse(""));
        Assert.Equal("a,b", WorldExtensionList.Format(new[] { "a", " b", "" }));
        Assert.Equal("", WorldExtensionList.Format(null));
    }

    [Fact]
    public void GameSetHash_ChangesWithExtensionsAndStaysTheSameWithout()
    {
        _sets.Config("Default", "default");
        _sets.Write("Default/Blocks/stone/block.json", "{ \"ID\": \"stone\" }");
        _sets.Config("Lamps", "lamps", "default");
        _sets.Write("Lamps/Blocks/lamp/block.json", "{ \"ID\": \"lamp\" }");

        string defaultFolder = Path.Combine(_sets.Root, "Default");
        var plain = GameSetStackResolver.Resolve(_sets.Root, "default", null).Stack;
        var extended = GameSetStackResolver.Resolve(_sets.Root, "default", new[] { "lamps" }).Stack;

        Assert.Equal(GameSetFolders.ComputeHash(defaultFolder), GameSetFolders.ComputeHash(plain));
        Assert.NotEqual(GameSetFolders.ComputeHash(plain), GameSetFolders.ComputeHash(extended));

        string before = GameSetFolders.ComputeHash(extended);
        _sets.Write("Lamps/Blocks/lamp/block.json", "{ \"ID\": \"lamp\", \"Name\": \"Changed\" }");
        Assert.NotEqual(before, GameSetFolders.ComputeHash(GameSetStackResolver.Resolve(_sets.Root, "default", new[] { "lamps" }).Stack));
    }

    [Fact]
    public void GameSetPack_ContainsTheExtensionFolders()
    {
        _sets.Config("Default", "default");
        _sets.Config("Lamps", "lamps", "default");
        _sets.Write("Lamps/Items/bulb/item.json", "{ \"ID\": \"bulb\" }");

        var stack = GameSetStackResolver.Resolve(_sets.Root, "default", new[] { "lamps" }).Stack;
        string target = Path.Combine(_sets.Root, "Download");
        Engine.Utils.DirectoryArchive.Extract(GameSetFolders.Pack(stack.Base.Folder, stack.Extensions), target);

        var downloaded = GameSetStackResolver.Resolve(target, "default", new[] { "lamps" });
        Assert.True(downloaded.Success, downloaded.ErrorText);
        Assert.Equal(GameSetFolders.ComputeHash(stack), GameSetFolders.ComputeHash(downloaded.Stack));
    }

    [Fact]
    public void GameSetIdIndex_IncludesTheWorldsExtensions()
    {
        _sets.Config("Default", "default");
        _sets.Write("Default/blocks.json", "[ { \"ID\": \"stone\" } ]");
        _sets.Config("Lamps", "lamps", "default");
        _sets.Write("Lamps/Blocks/lamp/block.json", "{ \"ID\": \"lamp\" }");
        _sets.Write("Lamps/Items/bulb/item.json", "{ \"ID\": \"bulb\" }");

        var plain = GameSetIdIndex.Load(_sets.Root, "default");
        var extended = GameSetIdIndex.Load(_sets.Root, "default", new[] { "lamps" });

        Assert.True(plain.IsBlock("default:stone"));
        Assert.False(plain.IsBlock("default:lamp"));
        Assert.True(extended.IsBlock("default:lamp"));
        Assert.True(extended.IsItem("default:bulb"));
        Assert.True(extended.IsItem("default:lamp"));
    }
}
