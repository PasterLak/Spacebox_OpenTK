using Spacebox.Network;

namespace Spacebox.Tests.Network;

public class BlockEditLimitsTests
{
    [Fact]
    public void Damage_IsClampedToTheStrongestDrillOrProjectile()
    {
        var limits = BlockEditLimits.Create(3, 40, null, 0);

        Assert.Equal(40, limits.MaxDamage);
        Assert.Equal(40, limits.ClampDamage(255, false, 100));
        Assert.Equal(5, limits.ClampDamage(5, false, 100));
    }

    [Fact]
    public void Damage_ToTransparentBlocks_MayBreakThemAtOnce()
    {
        var limits = BlockEditLimits.Create(2, 0, null, 0);

        Assert.Equal(60, limits.ClampDamage(60, true, 60));
        Assert.Equal(60, limits.ClampDamage(200, true, 60));
        Assert.Equal(2, limits.ClampDamage(60, false, 60));
    }

    [Fact]
    public void Damage_IsAtLeastOne()
    {
        Assert.Equal(1, BlockEditLimits.Create(0, 0, null, 0).MaxDamage);
    }

    [Fact]
    public void Repair_WithoutRepairTools_IsRejected()
    {
        var limits = BlockEditLimits.Create(1, 1, null, 100);

        Assert.Equal(0, limits.ClampRepair(10));
    }

    [Fact]
    public void Repair_IsLimitedByTheFastestToolRate()
    {
        var limits = BlockEditLimits.Create(1, 1, 2f, 100);

        Assert.Equal(13, limits.MaxRepair);
        Assert.Equal(13, limits.ClampRepair(255));
        Assert.Equal(4, limits.ClampRepair(4));
    }

    [Fact]
    public void Repair_LimitFitsInAByte()
    {
        var limits = BlockEditLimits.Create(1, 1, 0f, 10000);

        Assert.Equal(byte.MaxValue, limits.MaxRepair);
    }
}
