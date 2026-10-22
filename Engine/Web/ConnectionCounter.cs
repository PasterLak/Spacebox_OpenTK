namespace Engine.Web;

public sealed class ConnectionCounter
{
    private readonly int _limit;
    private readonly Dictionary<string, int> _open = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly object _lock = new object();

    public ConnectionCounter(int limit)
    {
        _limit = Math.Max(1, limit);
    }

    public bool TryEnter(string address)
    {
        if (IsUnlimited(address)) return true;

        lock (_lock)
        {
            _open.TryGetValue(address, out int count);
            if (count >= _limit) return false;
            _open[address] = count + 1;
            return true;
        }
    }

    public void Leave(string address)
    {
        if (IsUnlimited(address)) return;

        lock (_lock)
        {
            if (!_open.TryGetValue(address, out int count)) return;
            if (count <= 1) _open.Remove(address);
            else _open[address] = count - 1;
        }
    }

    public int OpenFor(string address)
    {
        lock (_lock) return _open.TryGetValue(address, out int count) ? count : 0;
    }

    private static bool IsUnlimited(string address)
    {
        return string.IsNullOrEmpty(address) || (System.Net.IPAddress.TryParse(address, out var ip) && System.Net.IPAddress.IsLoopback(ip));
    }
}
