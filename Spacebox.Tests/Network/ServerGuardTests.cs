using OpenTK.Mathematics;
using Spacebox.Network.Players;

namespace Spacebox.Tests.Network;

public class ServerGuardTests
{
    [Fact]
    public void Movement_AcceptsNormalSpeedAndRejectsTeleports()
    {
        var guard = new MovementGuard();
        Assert.True(guard.Accept(Vector3.Zero, 0f, 100.0));
        Assert.True(guard.Accept(new Vector3(20, 0, 0), 1f, 101.0));
        Assert.False(guard.Accept(new Vector3(5000, 0, 0), 2f, 102.0));
        Assert.Equal(new Vector3(20, 0, 0), guard.LastPosition);
    }

    [Fact]
    public void Movement_LagBurstUsesOwnerClock()
    {
        var guard = new MovementGuard();
        guard.Accept(Vector3.Zero, 0f, 100.0);
        Assert.True(guard.Accept(new Vector3(40, 0, 0), 1.5f, 100.05));
    }

    [Fact]
    public void Movement_OwnerClockCannotBeStretched()
    {
        var guard = new MovementGuard();
        guard.Accept(Vector3.Zero, 0f, 100.0);
        Assert.False(guard.Accept(new Vector3(1000, 0, 0), 1000f, 100.1));
    }

    [Fact]
    public void Movement_GraceAllowsJumpsAndRejectsNaN()
    {
        var guard = new MovementGuard();
        guard.Accept(Vector3.Zero, 0f, 100.0);
        guard.AllowJump(100.0, 3.0);
        Assert.True(guard.Accept(new Vector3(9000, 0, 0), 0.1f, 100.1));
        Assert.False(guard.Accept(new Vector3(float.NaN, 0, 0), 0.2f, 100.2));
        Assert.False(guard.Accept(new Vector3(0, 0, 0), 5f, 104.0));
    }

    [Fact]
    public void Gifts_OnlyTargetCanComplete()
    {
        var ledger = new GiftLedger();
        int id = ledger.Begin(2, 3, 77, 10.0);

        Assert.False(ledger.TryComplete(id, 2, out _));
        Assert.True(ledger.TryComplete(id, 3, out var gift));
        Assert.Equal(77, gift.RequestId);
        Assert.False(ledger.TryComplete(id, 3, out _));
    }

    [Fact]
    public void Gifts_ExpireAndClearForPeers()
    {
        var ledger = new GiftLedger();
        ledger.Begin(2, 3, 1, 10.0);
        ledger.Begin(4, 5, 2, 50.0);
        ledger.Begin(6, 2, 3, 50.0);

        var expired = new List<GiftLedger.Gift>();
        ledger.TakeExpired(20.0, expired);
        Assert.Single(expired);
        Assert.Equal(1, expired[0].RequestId);

        var left = new List<GiftLedger.Gift>();
        ledger.TakeForPeer(2, left);
        Assert.Single(left);
        Assert.Equal(1, ledger.Count);
    }

    [Fact]
    public void PeerRateLimiter_RefillsOverTime()
    {
        var limiter = new Engine.Networking.PeerRateLimiter(2, 1);
        Assert.True(limiter.TryConsume(1, 0));
        Assert.True(limiter.TryConsume(1, 0));
        Assert.False(limiter.TryConsume(1, 0.5));
        Assert.True(limiter.TryConsume(2, 0.5));
        Assert.True(limiter.TryConsume(1, 1.6));
        Assert.False(limiter.TryConsume(1, 1.6, 5));
    }
}
