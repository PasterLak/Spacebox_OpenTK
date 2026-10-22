using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Spacebox.Network.Dashboard;

public sealed class DashboardSessions
{
    public const string CookieName = "sbx_dashboard";
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);
    public static readonly TimeSpan MaxLifetime = TimeSpan.FromDays(7);
    private const int SweepEvery = 64;

    private readonly ConcurrentDictionary<string, (string User, DateTime Expires, DateTime Created)> _sessions = new ConcurrentDictionary<string, (string, DateTime, DateTime)>();
    private readonly Func<DateTime> _clock;
    private int _created;

    public DashboardSessions(Func<DateTime> clock = null)
    {
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    public string Create(string user)
    {
        var now = _clock();
        if (Interlocked.Increment(ref _created) % SweepEvery == 0) Sweep(now);

        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _sessions[token] = (user, now + Lifetime, now);
        return token;
    }

    public bool TryGet(string token, out string user)
    {
        user = null;
        if (string.IsNullOrEmpty(token) || !_sessions.TryGetValue(token, out var session)) return false;

        var now = _clock();
        if (IsExpired(session, now))
        {
            _sessions.TryRemove(token, out _);
            return false;
        }

        _sessions[token] = (session.User, now + Lifetime, session.Created);
        user = session.User;
        return true;
    }

    public void Remove(string token)
    {
        if (!string.IsNullOrEmpty(token)) _sessions.TryRemove(token, out _);
    }

    public void Clear() => _sessions.Clear();

    public int Count => _sessions.Count;

    private static bool IsExpired((string User, DateTime Expires, DateTime Created) session, DateTime now)
    {
        return session.Expires <= now || now - session.Created >= MaxLifetime;
    }

    private void Sweep(DateTime now)
    {
        foreach (var pair in _sessions)
            if (IsExpired(pair.Value, now)) _sessions.TryRemove(pair.Key, out _);
    }

    public static string CookieHeader(string token, bool secure = false)
    {
        return $"{CookieName}={token}; Path={DashboardPaths.Route}; HttpOnly; SameSite=Strict; Max-Age={(int)Lifetime.TotalSeconds}" + (secure ? "; Secure" : "");
    }

    public static string ExpiredCookieHeader()
    {
        return $"{CookieName}=; Path={DashboardPaths.Route}; HttpOnly; SameSite=Strict; Max-Age=0";
    }
}

public sealed class LoginThrottle
{
    public const int MaxFailures = 5;
    public const int MaxAttemptsPerMinute = 30;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan GlobalWindow = TimeSpan.FromMinutes(1);
    private const int SweepEvery = 64;

    private readonly ConcurrentDictionary<string, (int Failures, DateTime Since)> _failures = new ConcurrentDictionary<string, (int, DateTime)>();
    private readonly Queue<DateTime> _recentAttempts = new Queue<DateTime>();
    private readonly object _lock = new object();
    private readonly Func<DateTime> _clock;
    private int _calls;

    public LoginThrottle(Func<DateTime> clock = null)
    {
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    public static string KeyOf(string address)
    {
        if (string.IsNullOrEmpty(address) || !System.Net.IPAddress.TryParse(address, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6) return address ?? "";
        if (ip.IsIPv4MappedToIPv6) return ip.MapToIPv4().ToString();

        var bytes = ip.GetAddressBytes();
        for (int i = 8; i < bytes.Length; i++) bytes[i] = 0;
        return new System.Net.IPAddress(bytes) + "/64";
    }

    public bool IsLocked(string address)
    {
        string key = KeyOf(address);
        if (!_failures.TryGetValue(key, out var entry)) return false;
        if (_clock() - entry.Since > Window)
        {
            _failures.TryRemove(key, out _);
            return false;
        }
        return entry.Failures >= MaxFailures;
    }

    public bool TryBeginAttempt(string address)
    {
        var now = _clock();
        lock (_lock)
        {
            Sweep(now);
            while (_recentAttempts.Count > 0 && now - _recentAttempts.Peek() > GlobalWindow) _recentAttempts.Dequeue();
            bool local = Engine.Web.NetworkAddresses.IsLocal(address);
            if ((!local && _recentAttempts.Count >= MaxAttemptsPerMinute) || IsLocked(address)) return false;

            if (!local) _recentAttempts.Enqueue(now);
            RecordFailure(address);
            return true;
        }
    }

    public void RecordFailure(string address)
    {
        var now = _clock();
        _failures.AddOrUpdate(KeyOf(address), _ => (1, now), (_, entry) => now - entry.Since > Window ? (1, now) : (entry.Failures + 1, entry.Since));
    }

    public void Reset(string address) => _failures.TryRemove(KeyOf(address), out _);

    private void Sweep(DateTime now)
    {
        if (++_calls % SweepEvery != 0) return;
        foreach (var pair in _failures)
            if (now - pair.Value.Since > Window) _failures.TryRemove(pair.Key, out _);
    }
}
