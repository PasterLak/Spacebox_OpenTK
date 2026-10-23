using Spacebox.Game;
using Spacebox.Game.Player.Targeting;

namespace Spacebox.Tests.Game;

public sealed class BodyHighlightRulesTests
{
    private sealed class FakeDrillable : IDrillable
    {
        public void TakeDrillDamage(float damage)
        {
        }
    }

    [Fact]
    public void Drill_HighlightsDrillableTargetsInItsRange()
    {
        var drill = new DrillItem(1, "drill", 1f) { Id = 5, Range = 6f };
        var target = new FakeDrillable();

        Assert.Equal(BodyHighlightKind.Drill, BodyHighlightRules.Resolve(target, drill, 5f, 2f, false));
        Assert.Equal(BodyHighlightKind.None, BodyHighlightRules.Resolve(target, drill, 7f, 2f, false));
        Assert.Equal(6f, BodyHighlightRules.Range(drill, 2f));
    }

    [Fact]
    public void NothingHeldOrNotATarget_ShowsNothing()
    {
        var item = new Item(64, "stone", 1f) { Id = 3 };

        Assert.Equal(BodyHighlightKind.None, BodyHighlightRules.Resolve(new FakeDrillable(), item, 1f, 2f, true));
        Assert.Equal(BodyHighlightKind.None, BodyHighlightRules.Resolve(new object(), item, 1f, 2f, true));
        Assert.Equal(BodyHighlightKind.None, BodyHighlightRules.Resolve(new FakeDrillable(), null, 1f, 2f, true));
        Assert.Equal(2f, BodyHighlightRules.Range(item, 2f));
    }
}
