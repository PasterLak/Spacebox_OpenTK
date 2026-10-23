using Spacebox.Game.Resource.Content;
using static Spacebox.Tests.GameSets.Mock.MockGameSets;

namespace Spacebox.Tests.GameSets.Mock;

public sealed class MockExtensionErrorTests
{
    [Fact]
    public void ExtensionOfAMissingBase_IsRejectedButTheStackStillLoads()
    {
        var result = Resolve("mock_base", "mock_addon", "mock_missing_base");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Extension 'mock_missing_base': Made for mock_ghost_base, not for Mock Base"));
        Assert.Equal(new[] { "mock_addon" }, result.Stack.ExtensionIds);
        Assert.DoesNotContain(result.Stack.ReadContent().Blocks, b => b.Key.EndsWith("ghost_block"));
    }

    [Fact]
    public void ExtensionOfAMissingBase_SelectedAlone_AsksForABase()
    {
        var result = Resolve("mock_missing_base");

        Assert.False(result.Success);
        Assert.Null(result.Stack);
        Assert.Contains(result.Errors, e => e.Contains("extends 'mock_ghost_base': pick a base GameSet"));
    }

    [Fact]
    public void ExtensionForOneBase_IsRejectedOnAnother()
    {
        var onMock = ResolveWithDefault("mock_base", "mock_default_patch");
        var onFlat = Resolve("mock_flat", "mock_addon");

        Assert.Contains(onMock.Errors, e => e.Contains("Made for default, not for Mock Base"));
        Assert.Contains(onFlat.Errors, e => e.Contains("Made for mock_base, not for Mock Flat"));
        Assert.Empty(onFlat.Stack.Extensions);
    }

    [Fact]
    public void ExtensionNeedingANewerBase_IsRejected()
    {
        var result = Resolve("mock_base", "mock_too_new");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Needs Mock Base 2.0 or newer (installed: 1.2.0)"));
    }

    [Fact]
    public void ChildExtension_OnABaseItsParentDoesNotSupport_IsRejected()
    {
        var result = Resolve("mock_flat", "mock_child");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Extension 'mock_child'"));
        Assert.Empty(result.Stack.Extensions);
    }

    [Fact]
    public void MissingExtension_IsReportedAndTheBaseContentStillReads()
    {
        var result = Resolve("mock_base", "mock_gone");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Extension 'mock_gone' is not installed"));
        Assert.Equal(new[] { "mock_ore", "mock_stone" }, Keys(result.Stack.ReadContent().Blocks));
    }

    [Fact]
    public void SameShortIdInTwoExtensions_DoesNotClash()
    {
        var content = Read("mock_base", "mock_addon", "mock_collision");

        var gears = content.Items.Where(i => i.Key.EndsWith(":gear")).OrderBy(i => i.Key).ToArray();

        Assert.Equal(new[] { "mock_addon:gear", "mock_collision:gear" }, Keys(gears));
        Assert.Equal(new[] { "Addon Gear", "Collision Gear" }, gears.Select(Name).ToArray());
        Assert.DoesNotContain(content.Warnings, w => w.Contains("gear"));
    }

    [Fact]
    public void SameIdTwiceInOneExtension_KeepsTheFirstAndWarns()
    {
        var content = Read("mock_base", "mock_collision");

        var dup = Assert.Single(content.Blocks, b => b.Key.EndsWith("mock_dup"));

        Assert.Equal("mock_collision:mock_dup", dup.Key);
        Assert.Equal("First Dup", Name(dup));
        Assert.Contains(content.Warnings, w => w.Contains("'mock_collision:mock_dup' is defined twice in 'mock_collision'"));
    }

    [Fact]
    public void DuplicateModId_IsReportedAndOnlyTheFirstFolderIsUsed()
    {
        var installed = Installed();

        Assert.Contains("mock_addon", InstalledGameSets.DuplicateIds(installed));
        Assert.Equal("MockAddon", InstalledGameSets.Find(installed, "mock_addon").FolderName);
        var content = Read("mock_base", "mock_addon");
        Assert.DoesNotContain(content.Blocks, b => b.Key.EndsWith("copy_block"));
    }
}
