namespace Spacebox.Network.Dashboard;

public sealed class ExpiringKeySet
{
    private readonly Dictionary<string, DateTime> _expires = new Dictionary<string, DateTime>();
    private readonly int _capacity;
    private readonly TimeSpan _lifetime;

    public ExpiringKeySet(int capacity, TimeSpan lifetime)
    {
        _capacity = capacity;
        _lifetime = lifetime;
    }

    public int Count => _expires.Count;

    public void Add(string key, DateTime now)
    {
        if (!_expires.ContainsKey(key) && _expires.Count >= _capacity) MakeRoom(now);
        _expires[key] = now + _lifetime;
    }

    public bool Contains(string key, DateTime now)
    {
        if (!_expires.TryGetValue(key, out var expires)) return false;
        if (expires > now) return true;

        _expires.Remove(key);
        return false;
    }

    private void MakeRoom(DateTime now)
    {
        string oldest = null;
        var oldestExpires = DateTime.MaxValue;
        foreach (var pair in _expires)
        {
            if (pair.Value <= now) _expires.Remove(pair.Key);
            else if (pair.Value < oldestExpires)
            {
                oldest = pair.Key;
                oldestExpires = pair.Value;
            }
        }

        if (_expires.Count >= _capacity && oldest != null) _expires.Remove(oldest);
    }
}
