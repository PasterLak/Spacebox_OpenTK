using Spacebox.Network.Ships;

namespace Spacebox.Tests.Network;

public class ShipStateRelevanceTests
{
    [Theory]
    [InlineData(10f, ShipStateRelevance.NearInterval)]
    [InlineData(249f, ShipStateRelevance.NearInterval)]
    [InlineData(251f, ShipStateRelevance.MidInterval)]
    [InlineData(999f, ShipStateRelevance.MidInterval)]
    [InlineData(1001f, ShipStateRelevance.FarInterval)]
    public void Interval_GrowsWithDistance(float distance, int expected)
    {
        Assert.Equal(expected, ShipStateRelevance.IntervalFor(distance * distance));
    }

    [Theory]
    [InlineData(ShipStateRelevance.MidInterval)]
    [InlineData(ShipStateRelevance.FarInterval)]
    public void EachShip_IsDueOncePerInterval(int interval)
    {
        for (long id = -5; id < 40; id += 7)
        {
            int due = 0;
            for (long tick = 1; tick <= interval * 3; tick++)
                if (ShipStateRelevance.IsDue(tick, id, interval)) due++;
            Assert.Equal(3, due);
        }
    }

    [Fact]
    public void Ships_AreSpreadOverTicks()
    {
        int dueOnFirstTick = 0;
        for (long id = 0; id < 20; id++)
            if (ShipStateRelevance.IsDue(1, id, ShipStateRelevance.FarInterval)) dueOnFirstTick++;
        Assert.Equal(1, dueOnFirstTick);
        Assert.True(ShipStateRelevance.IsDue(7, 123, ShipStateRelevance.NearInterval));
    }
}
