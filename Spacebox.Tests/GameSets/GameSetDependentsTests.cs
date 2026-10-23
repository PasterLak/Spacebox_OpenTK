using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.GameSets;

public sealed class GameSetDependentsTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    private List<InstalledGameSet> Installed() => InstalledGameSets.Scan(_sets.Root);

    private void WriteSets()
    {
        _sets.Config("Empty", "empty");
        _sets.Config("Default", "default", "empty");
        _sets.Config("Weapons", "weapons", "*");
        _sets.Config("Lasers", "lasers", "weapons");
        _sets.Config("Tools", "tools", "default", "metals");
        _sets.Config("Metals", "metals", "default");
    }

    [Fact]
    public void DeletingAPack_ListsChildPacksAndWorldsThatUseThem()
    {
        WriteSets();
        var installed = Installed();
        var worlds = new[]
        {
            new WorldGameSetUse("Armory", "default", new[] { "lasers" }),
            new WorldGameSetUse("Plain", "default", Array.Empty<string>()),
            new WorldGameSetUse("Guns", "default", new[] { "weapons" })
        };

        var dependents = GameSetDependents.Find(InstalledGameSets.Find(installed, "weapons"), installed, worlds);

        Assert.Equal(new[] { "lasers" }, dependents.Mods.Select(m => m.Id).ToArray());
        Assert.Equal(new[] { "Armory", "Guns" }, dependents.Worlds.ToArray());
    }

    [Fact]
    public void IncludedPacks_AreDependenciesToo()
    {
        WriteSets();
        var installed = Installed();

        var dependents = GameSetDependents.Find(InstalledGameSets.Find(installed, "metals"), installed, new[] { new WorldGameSetUse("Workshop", "default", new[] { "tools" }) });

        Assert.Equal(new[] { "tools" }, dependents.Mods.Select(m => m.Id).ToArray());
        Assert.Equal(new[] { "Workshop" }, dependents.Worlds.ToArray());
    }

    [Fact]
    public void UnusedPacks_HaveNoDependents()
    {
        WriteSets();
        var installed = Installed();

        Assert.False(GameSetDependents.Find(InstalledGameSets.Find(installed, "lasers"), installed, new[] { new WorldGameSetUse("Plain", "default", Array.Empty<string>()) }).Any);
    }

    [Fact]
    public void EmptyAndDefault_CannotBeDeleted()
    {
        WriteSets();
        var installed = Installed();

        Assert.False(GameSetDependents.CanDelete(InstalledGameSets.Find(installed, "empty")));
        Assert.False(GameSetDependents.CanDelete(InstalledGameSets.Find(installed, "default")));
        Assert.True(GameSetDependents.CanDelete(InstalledGameSets.Find(installed, "weapons")));
    }
}
