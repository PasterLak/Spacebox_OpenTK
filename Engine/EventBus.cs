
namespace Engine
{
    public static class EventBus
    {
        static readonly Dictionary<Type, List<Subscription>> _subscriptions = new();
        static readonly object _sync = new();

        public static IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var sub = new Subscription(typeof(T), msg => handler((T)msg), Unsubscribe);
            lock (_sync)
            {
                if (!_subscriptions.TryGetValue(typeof(T), out var list))
                {
                    list = new List<Subscription>();
                    _subscriptions[typeof(T)] = list;
                }
                list.Add(sub);
            }
            return sub;
        }

        public static void Publish<T>(T message)
        {
            if (message == null) return;
            List<Subscription> list;
            lock (_sync)
            {
                if (!_subscriptions.TryGetValue(typeof(T), out list)) return;
                list = new List<Subscription>(list);
            }
            foreach (var sub in list)
            {
                try { sub.Handler(message); }
                catch { Debug.Error("[EventBus] error in Publish"); }
            }
        }

        public static void Clear()
        {
            lock (_sync)
            {
                _subscriptions.Clear();
            }
        }

        static void Unsubscribe(Subscription sub)
        {
            lock (_sync)
            {
                if (_subscriptions.TryGetValue(sub.EventType, out var list))
                {
                    list.Remove(sub);
                    if (list.Count == 0) _subscriptions.Remove(sub.EventType);
                }
            }
        }

        sealed class Subscription : IDisposable
        {
            public readonly Type EventType;
            public readonly Action<object> Handler;
            readonly Action<Subscription> _onDispose;
            bool _disposed;

            public Subscription(Type eventType, Action<object> handler, Action<Subscription> onDispose)
            {
                EventType = eventType;
                Handler = handler;
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _onDispose(this);
            }
        }
    }
}
