using Spacebox.Network.Items;
using Spacebox.Network.Messages;

namespace Spacebox.Tests.Network;

public class ItemLedgerTests
{
    private const short Iron = 1;
    private const short Stone = 2;

    private static Dictionary<short, int> Totals(params (short Id, int Count)[] items)
    {
        var totals = new Dictionary<short, int>();
        foreach (var (id, count) in items) totals[id] = count;
        return totals;
    }

    private static List<ItemAmount> Delta(short id, int amount) => new List<ItemAmount> { new ItemAmount { ItemId = id, Amount = amount } };

    [Fact]
    public void Snapshot_WithItemsTheServerNeverGave_ReturnsTheExcess()
    {
        var ledger = new ItemLedger(Totals((Iron, 5)));

        var excess = ledger.Snapshot(Totals((Iron, 9), (Stone, 3)), 0, 0f);

        Assert.Equal(4, excess[Iron]);
        Assert.Equal(3, excess[Stone]);
    }

    [Fact]
    public void Snapshot_WithFewerItems_AcceptsTheLoss()
    {
        var ledger = new ItemLedger(Totals((Iron, 5)));

        Assert.Empty(ledger.Snapshot(Totals((Iron, 2)), 0, 0f));
        Assert.Equal(2, ledger.Count(Iron));
        Assert.Equal(3, ledger.Snapshot(Totals((Iron, 5)), 0, 1f)[Iron]);
    }

    [Fact]
    public void Grant_NotYetAppliedByTheSnapshot_IsKept()
    {
        var ledger = new ItemLedger(Totals((Iron, 5)));
        int seq = ledger.Push(Delta(Iron, 10), 0f);

        Assert.Empty(ledger.Snapshot(Totals((Iron, 5)), seq - 1, 1f));
        Assert.Empty(ledger.Snapshot(Totals((Iron, 15)), seq, 2f));
        Assert.Equal(15, ledger.Count(Iron));
        Assert.Equal(0, ledger.PendingCount);
    }

    [Fact]
    public void Grant_CanBeUsedBeforeTheSnapshotConfirmsIt()
    {
        var ledger = new ItemLedger(Totals());
        int seq = ledger.Push(Delta(Iron, 4), 0f);

        Assert.True(ledger.TryLose(Iron, 3));
        Assert.Empty(ledger.Snapshot(Totals((Iron, 1)), seq, 1f));
        Assert.Equal(1, ledger.Count(Iron));
    }

    [Fact]
    public void Losses_BeyondWhatThePlayerHas_AreRefused()
    {
        var ledger = new ItemLedger(Totals((Iron, 2)));

        Assert.False(ledger.TryLose(Iron, 3));
        Assert.True(ledger.TryLose(Iron, 2));
        Assert.False(ledger.TryLose(Iron, 1));
        Assert.False(ledger.TryLose(Delta(Stone, 1)));
    }

    [Fact]
    public void Deduction_IsNotCountedTwice()
    {
        var ledger = new ItemLedger(Totals((Iron, 3)));
        int seq = ledger.Push(Delta(Iron, -1), 0f);

        Assert.Empty(ledger.Snapshot(Totals((Iron, 3)), seq - 1, 1f));
        Assert.False(ledger.TryLose(Iron, 3));
        Assert.Empty(ledger.Snapshot(Totals((Iron, 2)), seq, 2f));
        Assert.Equal(2, ledger.Count(Iron));
    }

    [Fact]
    public void Correction_SentAsPendingChange_IsNotRepeated()
    {
        var ledger = new ItemLedger(Totals((Iron, 1)));
        var excess = ledger.Snapshot(Totals((Iron, 5)), 0, 0f);
        int correction = ledger.Push(Delta(Iron, -excess[Iron]), 0f);

        Assert.Empty(ledger.Snapshot(Totals((Iron, 5)), correction - 1, 1f));
        Assert.False(ledger.TryLose(Iron, 2));
        Assert.Empty(ledger.Snapshot(Totals((Iron, 1)), correction, 2f));
    }

    [Fact]
    public void ChangesAClientNeverConfirms_CountAfterTheTimeout()
    {
        var ledger = new ItemLedger(Totals((Iron, 3)));
        ledger.Push(Delta(Iron, -3), 0f);

        Assert.Empty(ledger.Snapshot(Totals((Iron, 3)), 0, 1f));
        Assert.Equal(3, ledger.Snapshot(Totals((Iron, 3)), 0, ItemLedger.ForceIncludeAfter + 1f)[Iron]);
    }

    [Fact]
    public void TrustedSnapshot_ReplacesTheCounts()
    {
        var ledger = new ItemLedger(Totals((Iron, 1))) { TrustNextSnapshot = true };

        Assert.Empty(ledger.Snapshot(Totals((Iron, 64), (Stone, 9)), 0, 0f));
        Assert.Equal(64, ledger.Count(Iron));
        Assert.False(ledger.TrustNextSnapshot);
    }

    [Fact]
    public void ClientClaims_AddAndRemoveDirectly()
    {
        var ledger = new ItemLedger(Totals((Iron, 4)));

        Assert.True(ledger.TryLose(new List<ItemAmount> { new ItemAmount { ItemId = Iron, Amount = 2 }, new ItemAmount { ItemId = Iron, Amount = 2 } }));
        ledger.Gain(Stone, 1);

        Assert.Equal(0, ledger.Count(Iron));
        Assert.Equal(1, ledger.Count(Stone));
    }
}
