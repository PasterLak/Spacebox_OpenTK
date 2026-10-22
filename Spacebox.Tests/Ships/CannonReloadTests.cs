using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Tests.Ships;

public class CannonReloadTests
{
    private static CannonBlock CreateReadyCannon()
    {
        var data = new CannonBlockData(new BlockData("cannon", "cannon"), new CannonJSON { Projectile = "missing", ReloadTime = 0.5f, AimCone = 45, Power = 45 });
        return new CannonBlock(data) { IsActive = true, TriggerHeld = true };
    }

    [Fact]
    public void BlockedCannon_StaysReadyWithoutConsumingReload()
    {
        var cannon = CreateReadyCannon();

        Assert.True(cannon.UpdateCannon(0.1f));
        Assert.True(cannon.UpdateCannon(0.1f));
        Assert.Equal(1f, cannon.ReloadProgress);
    }

    [Fact]
    public void MarkFired_StartsReload()
    {
        var cannon = CreateReadyCannon();
        Assert.True(cannon.UpdateCannon(0.1f));

        cannon.MarkFired();

        Assert.False(cannon.UpdateCannon(0.1f));
        Assert.True(cannon.ReloadProgress < 1f);
        Assert.True(cannon.UpdateCannon(0.5f));
    }
}
