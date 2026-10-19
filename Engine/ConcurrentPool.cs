using System.Collections.Concurrent;

namespace Engine;

public sealed class ConcurrentPool<T> where T : class
{
    private readonly ConcurrentBag<T> _items = new ConcurrentBag<T>();
    private readonly Func<T> _factory;
    private readonly Action<T> _reset;
    private readonly int _maxRetained;
    private int _retained;

    public ConcurrentPool(Func<T> factory, Action<T> reset = null, int maxRetained = 16)
    {
        if (maxRetained < 0) throw new ArgumentOutOfRangeException(nameof(maxRetained));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _reset = reset;
        _maxRetained = maxRetained;
    }

    public int RetainedCount => Volatile.Read(ref _retained);
    public int MaxRetained => _maxRetained;

    public T Rent()
    {
        if (_items.TryTake(out var item))
        {
            Interlocked.Decrement(ref _retained);
            return item;
        }

        return _factory();
    }

    public void Return(T item)
    {
        if (item == null) return;

        _reset?.Invoke(item);

        if (Interlocked.Increment(ref _retained) > _maxRetained)
        {
            Interlocked.Decrement(ref _retained);
            return;
        }

        _items.Add(item);
    }
}
