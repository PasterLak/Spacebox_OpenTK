namespace Engine.Networking;

public sealed class PeerRateLimiter
{
    private readonly Dictionary<int, (double Tokens, double Time)> _buckets = new Dictionary<int, (double, double)>();

    public double Capacity { get; }
    public double RefillPerSecond { get; }

    public PeerRateLimiter(double capacity, double refillPerSecond)
    {
        Capacity = capacity;
        RefillPerSecond = refillPerSecond;
    }

    public bool TryConsume(int peer, double now, double cost = 1.0)
    {
        double tokens = Capacity;
        if (_buckets.TryGetValue(peer, out var bucket))
            tokens = Math.Min(Capacity, bucket.Tokens + Math.Max(0.0, now - bucket.Time) * RefillPerSecond);

        if (tokens < cost)
        {
            _buckets[peer] = (tokens, now);
            return false;
        }

        _buckets[peer] = (tokens - cost, now);
        return true;
    }

    public void Forget(int peer) => _buckets.Remove(peer);

    public void Clear() => _buckets.Clear();
}
