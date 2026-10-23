using Spacebox.Game.Resource;

namespace Spacebox.Tests.Game.Loot;

public sealed class LootRollerTests
{
    private static LootEntry Entry(string id, int probability, int min, int max) => new LootEntry { ItemIdStr = id, Probability = probability, MinCount = min, MaxCount = max };

    [Fact]
    public void SameSeed_GivesTheSameLoot()
    {
        var entries = new[] { Entry("a", 50, 1, 5), Entry("b", 50, 1, 5), Entry("c", 50, 1, 5) };

        var first = LootRoller.Roll(entries, 42, 255, _ => true);
        var second = LootRoller.Roll(entries, 42, 255, _ => true);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Rolls_RespectChanceCountsAndLimit()
    {
        var entries = new[] { Entry("always", 100, 3, 3), Entry("never", 0, 1, 1), Entry("rest", 100, 10, 10) };

        var rolls = LootRoller.Roll(entries, 7, 5, _ => true);

        Assert.Equal(new[] { new LootRoll("always", 3), new LootRoll("rest", 2) }, rolls);
    }

    [Fact]
    public void UnknownItems_AreSkipped()
    {
        var rolls = LootRoller.Roll(new[] { Entry("missing", 100, 1, 1), Entry("known", 100, 1, 1) }, 1, 255, id => id == "known");

        Assert.Equal(new[] { new LootRoll("known", 1) }, rolls);
    }
}
