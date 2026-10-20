using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Spacebox.Network.Dashboard;

public sealed class DashboardSessions
{
    public const string CookieName = "sbx_dashboard";
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);

    private readonly ConcurrentDictionary<string, (string User, DateTime Expires)> _sessions = new ConcurrentDictionary<string, (string, DateTime)>();
    private readonly Func<DateTime> _clock;

    public DashboardSessions(Func<DateTime> clock = null)
    {
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    public string Create(string user)
    {
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _sessions[token] = (user, _clock() + Lifetime);
        return token;
    }

    public bool TryGet(string token, out string user)
    {
        user = null;
        if (string.IsNullOrEmpty(token) || !_sessions.TryGetValue(token, out var session)) return false;

        if (session.Expires <= _clock())
        {
            _sessions.TryRemove(token, out _);
            return false;
        }

        _sessions[token] = (session.User, _clock() + Lifetime);
        user = session.User;
        return true;
    }

    public void Remove(string token)
    {
        if (!string.IsNullOrEmpty(token)) _sessions.TryRemove(token, out _);
    }

    public void Clear() => _sessions.Clear();

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
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, (int Failures, DateTime Since)> _failures = new ConcurrentDictionary<string, (int, DateTime)>();
    private readonly Func<DateTime> _clock;

    public LoginThrottle(Func<DateTime> clock = null)
    {
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    public bool IsLocked(string address)
    {
        if (!_failures.TryGetValue(address ?? "", out var entry)) return false;
        if (_clock() - entry.Since > Window)
        {
            _failures.TryRemove(address ?? "", out _);
            return false;
        }
        return entry.Failures >= MaxFailures;
    }

    public void RecordFailure(string address)
    {
        var now = _clock();
        _failures.AddOrUpdate(address ?? "", _ => (1, now), (_, entry) => now - entry.Since > Window ? (1, now) : (entry.Failures + 1, entry.Since));
    }

    public void Reset(string address) => _failures.TryRemove(address ?? "", out _);
}
