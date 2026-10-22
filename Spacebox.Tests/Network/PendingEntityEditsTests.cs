using Spacebox.Network;

namespace Spacebox.Tests.Network;

public class PendingEntityEditsTests
{
    [Fact]
    public void Edits_AreReplayedInOrderOnceTheEntityLoads()
    {
        var pending = new PendingEntityEdits<string>(10f, 8);
        var loaded = new HashSet<long>();
        var replayed = new List<string>();

        pending.TryAdd(2, 5, "a");
        pending.TryAdd(2, 6, "other");
        pending.TryAdd(3, 5, "b");

        pending.Update(1f, loaded.Contains, (_, m) => replayed.Add(m), (_, _) => { });
        Assert.Empty(replayed);
        Assert.True(pending.Has(5));

        loaded.Add(5);
        pending.Update(1f, loaded.Contains, (_, m) => replayed.Add(m), (_, _) => { });

        Assert.Equal(new[] { "a", "b" }, replayed);
        Assert.False(pending.Has(5));
        Assert.True(pending.Has(6));
    }

    [Fact]
    public void Edits_ExpireAfterTheTimeout()
    {
        var pending = new PendingEntityEdits<string>(2f, 8);
        var expired = new List<string>();
        pending.TryAdd(2, 5, "a");

        pending.Update(1.5f, _ => false, (_, _) => { }, (_, m) => expired.Add(m));
        Assert.Empty(expired);
        pending.Update(1f, _ => false, (_, _) => { }, (_, m) => expired.Add(m));

        Assert.Equal(new[] { "a" }, expired);
        Assert.Equal(0, pending.Count);
    }

    [Fact]
    public void EachPeer_HasALimit()
    {
        var pending = new PendingEntityEdits<string>(10f, 2);

        Assert.True(pending.TryAdd(2, 5, "a"));
        Assert.True(pending.TryAdd(2, 5, "b"));
        Assert.False(pending.TryAdd(2, 5, "c"));
        Assert.True(pending.TryAdd(3, 5, "d"));
    }

    [Fact]
    public void ReplayingAnEdit_DoesNotSeeItsOwnEntityAsPending()
    {
        var pending = new PendingEntityEdits<string>(10f, 8);
        bool pendingDuringReplay = true;
        pending.TryAdd(2, 5, "a");

        pending.Update(0.1f, _ => true, (_, _) => pendingDuringReplay = pending.Has(5), (_, _) => { });

        Assert.False(pendingDuringReplay);
    }
}
