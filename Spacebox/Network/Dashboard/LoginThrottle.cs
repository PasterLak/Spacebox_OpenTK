using System.Net;
using System.Net.Sockets;
using Engine.Web;

namespace Spacebox.Network.Dashboard;

public sealed class LoginThrottle
{
    public const int MaxFailures = 5;
    public const int GlobalFailuresPerMinute = 30;
    public const int MaxTrackedKeys = 4096;
    public const int MaxTrustedKeys = 64;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan MaxLockout = TimeSpan.FromHours(1);
    public static readonly TimeSpan StrikeMemory = TimeSpan.FromHours(6);
    public static readonly TimeSpan TrustLifetime = TimeSpan.FromDays(30);
    private const string SessionPrefix = "session:";

    private struct Entry
    {
        public int Failures;
        public int Strikes;
        public DateTime Since;
        public DateTime LastFailure;
        public DateTime LockedUntil;
    }

    private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();
    private readonly SlidingCounter _globalFailures = new SlidingCounter(TimeSpan.FromMinutes(1), 60);
    private readonly ExpiringKeySet _trusted = new ExpiringKeySet(MaxTrustedKeys, TrustLifetime);
    private readonly object _lock = new object();
    private readonly Func<DateTime> _clock;

    public LoginThrottle(Func<DateTime> clock = null)
    {
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    public int TrackedCount
    {
        get
        {
            lock (_lock) return _entries.Count;
        }
    }

    public bool UnderPressure
    {
        get
        {
            lock (_lock) return _globalFailures.Count(_clock()) >= GlobalFailuresPerMinute;
        }
    }

    public static string KeyOf(string address)
    {
        if (string.IsNullOrEmpty(address) || !IPAddress.TryParse(address, out var ip) || ip.AddressFamily != AddressFamily.InterNetworkV6) return address ?? "";
        if (ip.IsIPv4MappedToIPv6) return ip.MapToIPv4().ToString();

        var bytes = ip.GetAddressBytes();
        for (int i = 8; i < bytes.Length; i++) bytes[i] = 0;
        return new IPAddress(bytes) + "/64";
    }

    public bool IsLocked(string address)
    {
        var now = _clock();
        lock (_lock) return IsLocked(KeyOf(address), now);
    }

    public bool TryBeginAttempt(string address)
    {
        var now = _clock();
        string key = KeyOf(address);
        bool local = NetworkAddresses.IsLocal(address);
        lock (_lock)
        {
            if (IsLocked(key, now)) return false;
            if (!local && !_trusted.Contains(key, now) && _globalFailures.Count(now) >= GlobalFailuresPerMinute && HasRecentFailures(key, now)) return false;

            Fail(key, now);
            if (!local) _globalFailures.Add(now);
            return true;
        }
    }

    public bool TryBeginSessionAttempt(string sessionToken)
    {
        var now = _clock();
        string key = SessionPrefix + sessionToken;
        lock (_lock)
        {
            if (IsLocked(key, now)) return false;
            Fail(key, now);
            return true;
        }
    }

    public void RecordFailure(string address)
    {
        var now = _clock();
        lock (_lock) Fail(KeyOf(address), now);
    }

    public void RecordSuccess(string address)
    {
        var now = _clock();
        string key = KeyOf(address);
        bool local = NetworkAddresses.IsLocal(address);
        lock (_lock)
        {
            _entries.Remove(key);
            _trusted.Add(key, now);
            if (!local) _globalFailures.Remove(now);
        }
    }

    public void Reset(string address)
    {
        lock (_lock) _entries.Remove(KeyOf(address));
    }

    private bool IsLocked(string key, DateTime now)
    {
        return _entries.TryGetValue(key, out var entry) && now < entry.LockedUntil;
    }

    private bool HasRecentFailures(string key, DateTime now)
    {
        return _entries.TryGetValue(key, out var entry) && !IsExpired(entry, now) && (now < entry.LockedUntil || (entry.Failures > 0 && now - entry.Since <= Window));
    }

    private void Fail(string key, DateTime now)
    {
        if (!_entries.TryGetValue(key, out var entry) || IsExpired(entry, now))
        {
            if (!_entries.ContainsKey(key)) MakeRoom(now);
            entry = new Entry { Since = now };
        }

        if (now - entry.Since > Window)
        {
            entry.Failures = 0;
            entry.Since = now;
        }

        entry.Failures++;
        entry.LastFailure = now;
        if (entry.Failures >= MaxFailures)
        {
            entry.Strikes++;
            entry.LockedUntil = now + LockoutFor(entry.Strikes);
            entry.Failures = 0;
            entry.Since = entry.LockedUntil;
        }

        _entries[key] = entry;
    }

    private static TimeSpan LockoutFor(int strikes)
    {
        long ticks = Window.Ticks << Math.Min(strikes - 1, 10);
        return ticks >= MaxLockout.Ticks ? MaxLockout : TimeSpan.FromTicks(ticks);
    }

    private static bool IsExpired(Entry entry, DateTime now)
    {
        if (now < entry.LockedUntil) return false;
        return entry.Strikes == 0 ? now - entry.Since > Window : now - entry.LastFailure > StrikeMemory;
    }

    private void MakeRoom(DateTime now)
    {
        if (_entries.Count < MaxTrackedKeys) return;

        string oldest = null;
        var oldestFailure = DateTime.MaxValue;
        foreach (var pair in _entries)
        {
            if (IsExpired(pair.Value, now)) _entries.Remove(pair.Key);
            else if (pair.Value.LastFailure < oldestFailure)
            {
                oldest = pair.Key;
                oldestFailure = pair.Value.LastFailure;
            }
        }

        if (_entries.Count >= MaxTrackedKeys && oldest != null) _entries.Remove(oldest);
    }
}
