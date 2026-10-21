using System.Net;

namespace Engine.Networking.Query;

public sealed class QueryRateLimiter
{
    private const int MaxTrackedAddresses = 4096;

    private readonly Dictionary<IPAddress, (double Tokens, double Time)> _buckets = new Dictionary<IPAddress, (double, double)>();

    public int Capacity { get; }
    public int RefillPerSecond { get; }

    public QueryRateLimiter(int capacity, int refillPerSecond)
    {
        Capacity = capacity;
        RefillPerSecond = refillPerSecond;
    }

    public bool TryConsume(IPAddress address, int bytes, double now)
    {
        if (address == null) return false;
        if (_buckets.Count >= MaxTrackedAddresses && !_buckets.ContainsKey(address)) Prune(now);

        double tokens = Capacity;
        if (_buckets.TryGetValue(address, out var bucket))
            tokens = Math.Min(Capacity, bucket.Tokens + Math.Max(0.0, now - bucket.Time) * RefillPerSecond);

        if (tokens < bytes)
        {
            _buckets[address] = (tokens, now);
            return false;
        }

        _buckets[address] = (tokens - bytes, now);
        return true;
    }

    private void Prune(double now)
    {
        var stale = new List<IPAddress>();
        foreach (var pair in _buckets)
            if (pair.Value.Tokens + (now - pair.Value.Time) * RefillPerSecond >= Capacity) stale.Add(pair.Key);
        foreach (var address in stale) _buckets.Remove(address);
        if (_buckets.Count >= MaxTrackedAddresses) _buckets.Clear();
    }
}
