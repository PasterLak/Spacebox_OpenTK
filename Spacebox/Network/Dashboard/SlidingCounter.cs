namespace Spacebox.Network.Dashboard;

public sealed class SlidingCounter
{
    private readonly long[] _stamps;
    private readonly int[] _counts;
    private readonly long _bucketTicks;

    public SlidingCounter(TimeSpan window, int buckets)
    {
        _stamps = new long[buckets];
        _counts = new int[buckets];
        _bucketTicks = Math.Max(1, window.Ticks / buckets);
        Array.Fill(_stamps, long.MinValue);
    }

    public void Add(DateTime now)
    {
        long stamp = StampOf(now);
        int slot = SlotOf(stamp);
        if (_stamps[slot] != stamp)
        {
            _stamps[slot] = stamp;
            _counts[slot] = 0;
        }
        _counts[slot]++;
    }

    public void Remove(DateTime now)
    {
        long current = StampOf(now);
        for (int age = 0; age < _stamps.Length; age++)
        {
            long stamp = current - age;
            int slot = SlotOf(stamp);
            if (_stamps[slot] != stamp || _counts[slot] == 0) continue;
            _counts[slot]--;
            return;
        }
    }

    public int Count(DateTime now)
    {
        long current = StampOf(now);
        int total = 0;
        for (int i = 0; i < _stamps.Length; i++)
        {
            long age = current - _stamps[i];
            if (age >= 0 && age < _stamps.Length) total += _counts[i];
        }
        return total;
    }

    private long StampOf(DateTime now) => now.Ticks / _bucketTicks;

    private int SlotOf(long stamp) => (int)(stamp % _stamps.Length);
}
