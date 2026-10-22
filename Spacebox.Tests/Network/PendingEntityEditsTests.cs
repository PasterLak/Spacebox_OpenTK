using Spacebox.Network;

namespace Spacebox.Tests.Network;

public sealed class PendingEntityEditsTests
{
    [Fact]
    public void RemovePeer_DropsOnlyThatPeersEdits()
    {
        var edits = new PendingEntityEdits<string>();
        edits.TryAdd(2, 10, "a");
        edits.TryAdd(3, 10, "b");
        edits.TryAdd(2, 11, "c");

        var removed = edits.RemovePeer(2);

        Assert.Equal(new[] { "c", "a" }, removed);
        Assert.Equal(1, edits.Count);
        var replayed = new List<string>();
        edits.Update(0f, _ => true, (_, message) => replayed.Add(message), (_, _) => { });
        Assert.Equal(new[] { "b" }, replayed);
    }
}
