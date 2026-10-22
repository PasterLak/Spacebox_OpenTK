using System.Text.Json;
using System.Text.RegularExpressions;

namespace Spacebox.Tests;

public class DefaultDescriptionsTests
{
    public static IEnumerable<object[]> Files() => new[] { new object[] { "blocks.json" }, new object[] { "items.json" } };

    private static IEnumerable<(string Id, string Description)> Entries(string file)
    {
        foreach (var entry in DefaultGameSetContent.Flat(file).EnumerateArray())
        {
            string description = entry.TryGetProperty("Description", out var value) ? value.GetString() : null;
            yield return (entry.GetProperty("ID").GetString(), description);
        }
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void EveryEntry_HasAShortDescriptionWithoutNumbers(string file)
    {
        foreach (var (id, description) in Entries(file))
        {
            Assert.False(string.IsNullOrWhiteSpace(description), $"{file}: {id} has no description");
            Assert.False(Regex.IsMatch(description, @"\d"), $"{file}: {id} has numbers in its description, show them in the tooltip instead: {description}");
            Assert.True(description.Length <= 140, $"{file}: {id} description is too long");
        }
    }
}
