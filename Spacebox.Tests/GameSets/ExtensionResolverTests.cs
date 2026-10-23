using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.GameSets;

public sealed class ExtensionResolverTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    private List<InstalledGameSet> Installed() => InstalledGameSets.Scan(_sets.Root);

    private ExtensionResolution Resolve(params string[] requested)
    {
        var installed = Installed();
        return ExtensionResolver.Resolve(InstalledGameSets.Find(installed, "default"), installed, requested);
    }

    private static string[] Order(ExtensionResolution resolution) => resolution.Order.Select(e => e.Id).ToArray();

    [Fact]
    public void IncludesLoadBeforeTheExtensionThatNeedsThem()
    {
        _sets.Config("Default", "default");
        _sets.Config("Tools", "tools", "default", "metals");
        _sets.Config("Metals", "metals", "Default");
        _sets.Config("Lamps", "lamps", "default");

        var resolution = Resolve("lamps", "tools");

        Assert.True(resolution.Success, resolution.ErrorText);
        Assert.Equal(new[] { "lamps", "metals", "tools" }, Order(resolution));
    }

    [Fact]
    public void SharedDependenciesLoadOnce()
    {
        _sets.Config("Default", "default");
        _sets.Config("A", "a", "default", "core");
        _sets.Config("B", "b", "default", "core");
        _sets.Config("Core", "core", "default");

        var resolution = Resolve("a", "b", "a");

        Assert.True(resolution.Success);
        Assert.Equal(new[] { "core", "a", "b" }, Order(resolution));
    }

    [Fact]
    public void CyclesAreReportedWithThePath()
    {
        _sets.Config("Default", "default");
        _sets.Config("A", "a", "default", "b");
        _sets.Config("B", "b", "default", "c");
        _sets.Config("C", "c", "default", "a");

        var resolution = Resolve("a");

        Assert.False(resolution.Success);
        Assert.Contains(resolution.Errors, e => e.Contains("a -> b -> c -> a"));
    }

    [Fact]
    public void MissingDependenciesAndExtensionsAreClearErrors()
    {
        _sets.Config("Default", "default");
        _sets.Config("A", "a", "default", "ghost");

        var resolution = Resolve("a", "missing");

        Assert.False(resolution.Success);
        Assert.Contains(resolution.Errors, e => e.Contains("'a' includes 'ghost'"));
        Assert.Contains(resolution.Errors, e => e.Contains("'missing' is not installed"));
    }

    [Fact]
    public void ExtensionsOfAnotherBaseAreRejected()
    {
        _sets.Config("Default", "default");
        _sets.Config("Other", "other");
        _sets.Config("ForOther", "for_other", "other");

        var resolution = Resolve("for_other");

        Assert.False(resolution.Success);
        Assert.Contains(resolution.Errors, e => e.Contains("Made for other"));
    }

    [Fact]
    public void BaseGameSetsCannotBeUsedAsExtensions()
    {
        _sets.Config("Default", "default");
        _sets.Config("Other", "other");

        var resolution = Resolve("other");

        Assert.False(resolution.Success);
        Assert.Contains(resolution.Errors, e => e.Contains("not an extension"));
    }

    [Fact]
    public void StackResolver_SelectingAnExtensionLoadsItsBase()
    {
        _sets.Config("Default", "default");
        _sets.Config("Lamps", "lamps", "default");

        var result = GameSetStackResolver.Resolve(ModFolders.Flat(_sets.Root), "lamps", null);

        Assert.True(result.Success, result.ErrorText);
        Assert.Equal("default", result.Stack.Base.Id);
        Assert.Equal(new[] { "lamps" }, result.Stack.ExtensionIds);
        Assert.Null(result.Stack.Fallback);
        Assert.Equal(2, result.Stack.Layers.Count);
    }

    [Fact]
    public void StackResolver_NonDefaultBaseFallsBackToDefault()
    {
        _sets.Config("Default", "default");
        _sets.Config("Other", "other");

        var result = GameSetStackResolver.Resolve(ModFolders.Flat(_sets.Root), "other", Array.Empty<string>());

        Assert.True(result.Success);
        Assert.NotNull(result.Stack.Fallback);
        Assert.Equal(new[] { Path.Combine(_sets.Root, "Other"), Path.Combine(_sets.Root, "Default") }, result.Stack.SearchRootsNewestFirst());
    }

    [Fact]
    public void StackResolver_UnknownBaseFails()
    {
        _sets.Config("Default", "default");

        var result = GameSetStackResolver.Resolve(ModFolders.Flat(_sets.Root), "nope", null);

        Assert.False(result.Success);
        Assert.Null(result.Stack);
    }
}
