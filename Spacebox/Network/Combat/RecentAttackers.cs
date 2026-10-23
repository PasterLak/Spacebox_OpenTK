namespace Spacebox.Network.Combat;

public sealed class RecentAttackers
{
    private readonly Dictionary<int, Dictionary<int, double>> _hits = new Dictionary<int, Dictionary<int, double>>();
    private readonly double _memorySeconds;

    public RecentAttackers(double memorySeconds)
    {
        _memorySeconds = memorySeconds;
    }

    public void Register(int victim, int attacker, double now)
    {
        if (victim == attacker) return;

        if (!_hits.TryGetValue(victim, out var attackers))
        {
            attackers = new Dictionary<int, double>();
            _hits[victim] = attackers;
        }
        attackers[attacker] = now;
    }

    public bool WasRecentlyHitBy(int victim, int attacker, double now)
    {
        if (victim == attacker || !_hits.TryGetValue(victim, out var attackers)) return false;
        return attackers.TryGetValue(attacker, out double time) && now >= time && now - time <= _memorySeconds;
    }

    public void ForgetVictim(int victim) => _hits.Remove(victim);

    public void ForgetPeer(int peer)
    {
        _hits.Remove(peer);
        foreach (var attackers in _hits.Values) attackers.Remove(peer);
    }

    public void Clear() => _hits.Clear();
}
