namespace Engine.Tests.Core;

[Collection(GlobalStateCollection.Name)]
public class EventBusTests
{
    private struct ScoreEvent : IEvent
    {
        public int Score;
    }

    private struct UnsubscribeEvent : IEvent
    {
        public int Value;
    }

    private sealed class Listener<T> : IEventListener<T> where T : struct, IEvent
    {
        public int Calls;
        public Action<T>? OnReceived;

        public void OnEvent(ref T eventData)
        {
            Calls++;
            OnReceived?.Invoke(eventData);
        }
    }

    [Fact]
    public void Publish_ReachesEverySubscriberOnce()
    {
        var first = new Listener<ScoreEvent>();
        var second = new Listener<ScoreEvent>();
        int received = 0;
        first.OnReceived = e => received = e.Score;

        EventBus.Subscribe(first);
        EventBus.Subscribe(first);
        EventBus.Subscribe(second);
        try
        {
            var evt = new ScoreEvent { Score = 7 };
            EventBus.Publish(ref evt);

            Assert.Equal(1, first.Calls);
            Assert.Equal(1, second.Calls);
            Assert.Equal(7, received);
        }
        finally
        {
            EventBus.Unsubscribe(first);
            EventBus.Unsubscribe(second);
        }
    }

    [Fact]
    public void Unsubscribe_StopsDelivery()
    {
        var listener = new Listener<UnsubscribeEvent>();
        EventBus.Subscribe(listener);
        EventBus.Unsubscribe(listener);

        var evt = new UnsubscribeEvent { Value = 1 };
        EventBus.Publish(ref evt);

        Assert.Equal(0, listener.Calls);
    }
}
