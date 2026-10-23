using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.GameSets;

public sealed class ExtensionCompatibilityTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    private List<InstalledGameSet> Installed() => InstalledGameSets.Scan(_sets.Root);

    private InstalledGameSet Find(string id) => InstalledGameSets.Find(Installed(), id);

    private void WriteBaseSets(string defaultVersion = "0.1.5")
    {
        _sets.Config("Empty", "empty");
        _sets.Write("Default/config.json", $"{{ \"ModId\": \"default\", \"Version\": \"{defaultVersion}\", \"Extends\": \"empty\" }}");
        _sets.Write("Mine/config.json", "{ \"ModId\": \"mine\", \"Version\": \"2.0\", \"Extends\": \"empty\" }");
    }

    [Theory]
    [InlineData("0.1.5", "0.1.5", 0)]
    [InlineData("0.1.10", "0.1.9", 1)]
    [InlineData("0.2", "0.1.9", 1)]
    [InlineData("1.0", "1", 0)]
    [InlineData("v0.1.4", "0.1.5", -1)]
    public void Versions_CompareNumberByNumber(string a, string b, int expected)
    {
        Assert.Equal(expected, GameSetVersion.Compare(a, b));
    }

    [Fact]
    public void OutdatedBase_IsRefusedWithAReason()
    {
        WriteBaseSets("0.1.4");
        _sets.Write("Pack/config.json", "{ \"ModId\": \"pack\", \"Extends\": \"default\", \"MinBaseVersion\": \"0.1.5\" }");

        string problem = ExtensionCompatibility.Problem(Find("pack"), Find("default"));

        Assert.Contains("0.1.5 or newer", problem);
        var resolution = ExtensionResolver.Resolve(Find("default"), Installed(), new[] { "pack" });
        Assert.False(resolution.Success);
    }

    [Fact]
    public void TooNewBase_IsRefusedWhenAMaximumIsSet()
    {
        WriteBaseSets("0.2.0");
        _sets.Write("Pack/config.json", "{ \"ModId\": \"pack\", \"Extends\": \"default\", \"MinBaseVersion\": \"0.1.5\", \"MaxBaseVersion\": \"0.1.9\" }");

        Assert.Contains("up to 0.1.9", ExtensionCompatibility.Problem(Find("pack"), Find("default")));
    }

    [Fact]
    public void SupportedBase_IsAccepted()
    {
        WriteBaseSets("0.1.6");
        _sets.Write("Pack/config.json", "{ \"ModId\": \"pack\", \"Extends\": \"default\", \"MinBaseVersion\": \"0.1.5\" }");

        Assert.Null(ExtensionCompatibility.Problem(Find("pack"), Find("default")));
    }

    [Fact]
    public void AnyGameSetExtensions_WorkWithEveryGameSetButEmpty()
    {
        WriteBaseSets();
        _sets.Write("Weapons/config.json", "{ \"ModId\": \"weapons\", \"Extends\": \"*\" }");
        var installed = Installed();

        Assert.True(Find("weapons").IsExtension);
        Assert.Contains(installed, g => g.Id == "weapons");
        Assert.Single(InstalledGameSets.ExtensionsFor(installed, Find("default")));
        Assert.Single(InstalledGameSets.ExtensionsFor(installed, Find("mine")));
        Assert.Empty(InstalledGameSets.ExtensionsFor(installed, Find("empty")));
    }

    [Fact]
    public void ListedTargets_AreReadFromAJsonArray()
    {
        WriteBaseSets();
        _sets.Write("Weapons/config.json", "{ \"ModId\": \"weapons\", \"Extends\": [ \"default\", \"other\" ] }");
        var installed = Installed();

        Assert.Equal(new[] { "default", "other" }, ExtensionTargets.Parse(Find("weapons").Config.Extends));
        Assert.Single(InstalledGameSets.ExtensionsFor(installed, Find("default")));
        Assert.Empty(InstalledGameSets.ExtensionsFor(installed, Find("mine")));
        Assert.Contains("Made for default, other", ExtensionCompatibility.Problem(Find("weapons"), Find("mine")));
    }

    [Fact]
    public void OpeningAnAnyGameSetExtension_UsesDefaultAsItsBase()
    {
        WriteBaseSets();
        _sets.Write("Weapons/config.json", "{ \"ModId\": \"weapons\", \"Extends\": \"*\" }");

        var result = GameSetStackResolver.Resolve(ModFolders.Flat(_sets.Root), "weapons", null);

        Assert.True(result.Success, result.ErrorText);
        Assert.Equal("default", result.Stack.Base.Id);
        Assert.Equal("weapons", Assert.Single(result.Stack.Extensions).Id);
    }
}
