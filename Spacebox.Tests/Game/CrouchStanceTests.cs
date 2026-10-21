using Spacebox.Game.Player;

namespace Spacebox.Tests;

public class CrouchStanceTests
{
    private static CrouchStance Create() => new CrouchStance(GravityWalker.EyeHeight, GravityWalker.CrouchEyeHeight);

    [Fact]
    public void StartsStanding()
    {
        var stance = Create();

        Assert.False(stance.IsCrouching);
        Assert.Equal(GravityWalker.EyeHeight, stance.EyeHeight);
    }

    [Fact]
    public void CrouchLowersEyeGraduallyToCrouchHeight()
    {
        var stance = Create();

        stance.Update(true, 0.05f, _ => true);
        Assert.True(stance.IsCrouching);
        Assert.True(stance.EyeHeight > GravityWalker.CrouchEyeHeight);

        for (int i = 0; i < 20; i++) stance.Update(true, 0.05f, _ => true);
        Assert.Equal(GravityWalker.CrouchEyeHeight, stance.EyeHeight, 4);
    }

    [Fact]
    public void StaysCrouchedWithoutHeadroom()
    {
        var stance = Create();
        stance.Reset(GravityWalker.CrouchEyeHeight);

        stance.Update(false, 0.1f, _ => false);

        Assert.Equal(GravityWalker.CrouchEyeHeight, stance.EyeHeight, 4);
    }

    [Fact]
    public void StandsUpWhenThereIsRoom()
    {
        var stance = Create();
        stance.Reset(GravityWalker.CrouchEyeHeight);

        for (int i = 0; i < 20; i++) stance.Update(false, 0.05f, _ => true);

        Assert.False(stance.IsCrouching);
    }

    [Fact]
    public void CrouchedPlayerFitsIntoOneBlockGap()
    {
        Assert.True(GravityWalker.CrouchEyeHeight + GravityWalker.HeadClearance < 1f);
        Assert.True(GravityWalker.CapsuleHeight > 1f);
    }
}
