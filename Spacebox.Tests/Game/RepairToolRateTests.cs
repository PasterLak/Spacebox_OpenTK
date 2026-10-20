using Spacebox.Game;

namespace Spacebox.Tests.Game;

public class RepairToolRateTests
{
    [Theory]
    [InlineData(100, 1f, 100f)]
    [InlineData(800, 1f, 800f)]
    [InlineData(100, 0.5f, 200f)]
    public void GetRepairRate_RepairsFullDurabilityWithinRepairTime(int maxDurability, float repairTime, float expected)
    {
        var tool = new RepairToolItem(1, "Repair Tool", 1f) { RepairTime = repairTime };

        Assert.Equal(expected, tool.GetRepairRate(maxDurability), 3);
    }

    [Fact]
    public void GetRepairRate_WithoutDurability_IsZero()
    {
        var tool = new RepairToolItem(1, "Repair Tool", 1f);

        Assert.Equal(0f, tool.GetRepairRate(0));
    }

    [Fact]
    public void DefaultRepairTime_IsOneSecond()
    {
        Assert.Equal(1f, new RepairToolItem(1, "Repair Tool", 1f).RepairTime);
    }
}
