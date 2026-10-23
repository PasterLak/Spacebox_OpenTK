using Spacebox.Network.Combat;

namespace Spacebox.Tests.Network;

public class RecentAttackersTests
{
    [Fact]
    public void Attacker_IsRememberedForTheMemoryWindow()
    {
        var attackers = new RecentAttackers(10);
        attackers.Register(2, 3, 100);

        Assert.True(attackers.WasRecentlyHitBy(2, 3, 105));
        Assert.True(attackers.WasRecentlyHitBy(2, 3, 110));
        Assert.False(attackers.WasRecentlyHitBy(2, 3, 110.5));
    }

    [Fact]
    public void UnknownOrSelfAttacker_IsNotAccepted()
    {
        var attackers = new RecentAttackers(10);
        attackers.Register(2, 3, 100);
        attackers.Register(4, 4, 100);

        Assert.False(attackers.WasRecentlyHitBy(2, 5, 101));
        Assert.False(attackers.WasRecentlyHitBy(3, 2, 101));
        Assert.False(attackers.WasRecentlyHitBy(4, 4, 101));
    }

    [Fact]
    public void ForgetVictim_ClearsItsAttackers()
    {
        var attackers = new RecentAttackers(10);
        attackers.Register(2, 3, 100);
        attackers.ForgetVictim(2);

        Assert.False(attackers.WasRecentlyHitBy(2, 3, 101));
    }

    [Fact]
    public void ForgetPeer_RemovesItAsVictimAndAttacker()
    {
        var attackers = new RecentAttackers(10);
        attackers.Register(2, 3, 100);
        attackers.Register(3, 2, 100);
        attackers.Register(4, 2, 100);
        attackers.ForgetPeer(2);

        Assert.False(attackers.WasRecentlyHitBy(2, 3, 101));
        Assert.False(attackers.WasRecentlyHitBy(3, 2, 101));
        Assert.False(attackers.WasRecentlyHitBy(4, 2, 101));
    }
}
