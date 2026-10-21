using Spacebox.Game.GUI;

namespace Spacebox.Tests.Game;

public class NameSuggestionsTests
{
    private static readonly string[] Players = { "Anna", "Bob", "Annette", "Joanna", "Me" };

    [Fact]
    public void PrefixMatchesComeBeforeContainedMatches()
    {
        var results = new List<string>();
        NameSuggestions.Filter("ann", Players, System.Array.Empty<string>(), results, 10);
        Assert.Equal(new[] { "Anna", "Annette", "Joanna" }, results);
    }

    [Fact]
    public void ExcludedAndExactNamesAreSkipped()
    {
        var results = new List<string>();
        NameSuggestions.Filter("anna", Players, new[] { "Me", "joanna" }, results, 10);
        Assert.Empty(results);
    }

    [Fact]
    public void EmptyTextListsEveryoneUpToTheLimit()
    {
        var results = new List<string>();
        NameSuggestions.Filter("", Players, new[] { "Me" }, results, 3);
        Assert.Equal(new[] { "Anna", "Bob", "Annette" }, results);
    }
}
