using Spacebox.Game.Player;

namespace Spacebox.Tests.Combat;

public sealed class ProjectileTargetsTests
{
    [Fact]
    public void Shooter_IsIgnoredOnlyUntilTheFirstRicochet()
    {
        Assert.Equal(5, ProjectileTargets.IgnoredPeer(5, 0));
        Assert.Equal(ProjectileTargets.NoIgnoredPeer, ProjectileTargets.IgnoredPeer(5, 1));
        Assert.Equal(ProjectileTargets.NoIgnoredPeer, ProjectileTargets.IgnoredPeer(0, 3));
    }

    [Fact]
    public void DeathCause_NamesOwnShots()
    {
        Assert.Equal("Hit by their own shot", ProjectileTargets.DeathCause("Anna", true, false));
        Assert.Equal("Killed by Bob", ProjectileTargets.DeathCause("Bob", false, false));
        Assert.Equal("Headshot by Bob", ProjectileTargets.DeathCause("Bob", false, true));
    }
}
