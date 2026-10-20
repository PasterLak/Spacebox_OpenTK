using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
    public interface IEvent { }

    public interface IEventListener<T> where T : struct, IEvent
    {
        void OnEvent(ref T eventData);
    }

    public static class EventBus
    {
        private static readonly object _registryLock = new object();
        private static readonly List<Action> _clearActions = new List<Action>();

        public static void RegisterClearAction(Action action)
        {
            lock (_registryLock)
            {
                _clearActions.Add(action);
            }
        }

        public static void ClearAll()
        {
            lock (_registryLock)
            {
                for (int i = 0; i < _clearActions.Count; i++)
                {
                    _clearActions[i]();
                }
            }
        }

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
        private static readonly object _lock = new object();
        private static IEventListener<T>[] _listeners = Array.Empty<IEventListener<T>>();

        static EventBusInternal()
        {
            EventBus.RegisterClearAction(Clear);
        }

        public static void Subscribe(IEventListener<T> listener)
        {
            lock (_lock)
            {
                if (!_listeners.Contains(listener))
                {
                    var list = _listeners.ToList();
                    list.Add(listener);
                    _listeners = list.ToArray();
                }
            }
        }

        public static void Unsubscribe(IEventListener<T> listener)
        {
            lock (_lock)
            {
                if (_listeners.Contains(listener))
                {
                    var list = _listeners.ToList();
                    list.Remove(listener);
                    _listeners = list.ToArray();
                }
            }
        }

        public static void Publish(ref T eventData)
        {
            var currentListeners = _listeners;
            for (int i = 0; i < currentListeners.Length; i++)
            {
                currentListeners[i].OnEvent(ref eventData);
            }
        }

        private static void Clear()
        {
            lock (_lock)
            {
                _listeners = Array.Empty<IEventListener<T>>();
            }
        }
    }
}