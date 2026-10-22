using Spacebox.Network.Dedicated;

namespace Spacebox.Tests.Network;

public class ServerShutdownSignalsTests
{
    [Fact]
    public void Request_StopsOnceOnTheMainThreadQueue()
    {
        var queue = new Queue<Action>();
        int stops = 0;
        using var signals = new ServerShutdownSignals(queue.Enqueue, () => stops++);

        Assert.True(signals.Request());
        Assert.False(signals.Request());
        Assert.Equal(0, stops);
        Assert.False(signals.IsStopped);

        while (queue.Count > 0) queue.Dequeue()();

        Assert.Equal(1, stops);
        Assert.True(signals.WaitUntilStopped(TimeSpan.Zero));
    }

    [Fact]
    public void Request_CountsAsStoppedEvenWhenStopThrows()
    {
        var queue = new Queue<Action>();
        using var signals = new ServerShutdownSignals(queue.Enqueue, () => throw new InvalidOperationException());

        signals.Request();
        Assert.Throws<InvalidOperationException>(() => queue.Dequeue()());

        Assert.True(signals.IsStopped);
    }
}
