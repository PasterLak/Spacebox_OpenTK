using System.Collections.Generic;

namespace Engine
{

    public interface IEvent { }

    public interface IEventListener<T> where T : struct, IEvent
    {
        void OnEvent(ref T eventData);
    }

    public static class EventBus
    {
        public static void Subscribe<T>(IEventListener<T> listener) where T : struct, IEvent
        {
            EventBusInternal<T>.Subscribe(listener);
        }

        public static void Unsubscribe<T>(IEventListener<T> listener) where T : struct, IEvent
        {
            EventBusInternal<T>.Unsubscribe(listener);
        }

        public static void Publish<T>(ref T eventData) where T : struct, IEvent
        {
            EventBusInternal<T>.Publish(ref eventData);
        }

    }

    internal static class EventBusInternal<T> where T : struct, IEvent
    {
        private static readonly List<IEventListener<T>> _listeners = new List<IEventListener<T>>();

        public static void Subscribe(IEventListener<T> listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public static void Unsubscribe(IEventListener<T> listener)
        {
            _listeners.Remove(listener);
        }

        public static void Publish(ref T eventData)
        {
            for (int i = 0; i < _listeners.Count; i++)
            {
                _listeners[i].OnEvent(ref eventData);
            }
        }

        public static void Clear()
        {
            _listeners.Clear();
        }
    }
}
