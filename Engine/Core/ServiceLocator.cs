
namespace Engine;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
    private static readonly object _lock = new object();

    public static void Register<T>(T service)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));

        lock (_lock)
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                throw new InvalidOperationException($"Service of type {type.Name} is already registered.");
            }
            _services[type] = service;
        }
    }

    public static void Unregister<T>()
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
            }
        }
    }

    public static T Get<T>()
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                if(service == null)
                {
                    throw new InvalidOperationException($"Service of type {type.Name} is registered but is null.");
                }

                return (T)service;
            }

            throw new KeyNotFoundException($"Service of type {type.Name} is not registered.");
        }
    }

    public static bool TryGet<T>(out T service)
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var instance))
            {
                service = (T)instance;

                if (service == null)
                {
                    throw new InvalidOperationException($"Service of type {type.Name} is registered but is null.");
                }

                return true;
            }

            service = default;
            return false;
        }
    }

    public static void Reset()
    {
        lock (_lock)
        {
            foreach (var service in _services.Values)
            {
                if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _services.Clear();
        }
    }
}