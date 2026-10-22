using Spacebox.Network.Players;

namespace Spacebox.Tests.Network;

public class PlayerDataFlushTests
{
    [Fact]
    public void Completes_WhenEveryPlayerAnswered()
    {
        var flush = new PlayerDataFlush(2f);
        int requests = 0, completed = 0;

        flush.Start(new[] { 2, 3 }, () => requests++, () => completed++);
        flush.Received(2);
        Assert.Equal(0, completed);
        flush.Received(3);

        Assert.Equal(1, requests);
        Assert.Equal(1, completed);
        Assert.False(flush.IsRunning);
    }

    [Fact]
    public void Completes_AfterTheTimeout()
    {
        var flush = new PlayerDataFlush(2f);
        int completed = 0;
        flush.Start(new[] { 2 }, () => { }, () => completed++);

        Assert.False(flush.Update(1.5f));
        Assert.True(flush.Update(1f));
        Assert.Equal(1, completed);
        Assert.False(flush.Update(1f));
    }

    [Fact]
    public void WithoutRemotePlayers_CompletesImmediatelyWithoutRequest()
    {
        var flush = new PlayerDataFlush();
        int requests = 0, completed = 0;

        flush.Start(Array.Empty<int>(), () => requests++, () => completed++);

        Assert.Equal(0, requests);
        Assert.Equal(1, completed);
    }

    [Fact]
    public void UnknownOrRepeatedAnswers_AreIgnored()
    {
        var flush = new PlayerDataFlush();
        int completed = 0;
        flush.Start(new[] { 2, 3 }, () => { }, () => completed++);

        flush.Received(9);
        flush.Received(2);
        flush.Received(2);

        Assert.Equal(0, completed);
        Assert.Equal(1, flush.PendingCount);
    }
}
