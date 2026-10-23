using Spacebox.Game.Player;

namespace Spacebox.Tests.Game;

public class ProjectileRicochetTests
{
    [Theory]
    [InlineData(30f, 2, 0, 20f, true)]
    [InlineData(30f, 2, 0, 45f, false)]
    [InlineData(30f, 2, 2, 10f, false)]
    [InlineData(0f, 2, 0, 5f, false)]
    public void ShouldRicochet_FollowsAngleAndCount(float maxAngle, int possible, int done, float angle, bool expected)
    {
        Assert.Equal(expected, Projectile.ShouldRicochet(maxAngle, possible, done, angle));
    }
}
