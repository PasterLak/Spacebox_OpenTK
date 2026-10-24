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
