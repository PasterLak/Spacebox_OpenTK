using System.Diagnostics;
using Engine;

namespace Spacebox.Network.Dashboard;

public readonly record struct MetricsSample(long Time, double Cpu, double RamMb, double HeapMb, int Tps, double UpdateMs, int Players);

public sealed class DashboardMetrics : IDisposable
{
    public const int HistorySize = 180;
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    private readonly MetricsSample[] _history = new MetricsSample[HistorySize];
    private readonly object _lock = new object();
    private readonly Stopwatch _wall = Stopwatch.StartNew();
    private readonly Func<int> _players;
    private Timer _timer;
    private TimeSpan _lastCpu;
    private double _lastWall;
    private int _count;
    private int _next;

    public DashboardMetrics(Func<int> players)
    {
        _players = players;
        using var process = Process.GetCurrentProcess();
        _lastCpu = process.TotalProcessorTime;
        _lastWall = _wall.Elapsed.TotalSeconds;
    }

    public static DateTime StartTime { get; } = DateTime.Now;

    public void Start()
    {
        _timer ??= new Timer(_ => Sample(), null, TimeSpan.Zero, Interval);
    }

    public MetricsSample Latest
    {
        get
        {
            lock (_lock) return _count == 0 ? default : _history[(_next - 1 + HistorySize) % HistorySize];
        }
    }

    public List<MetricsSample> History()
    {
        var result = new List<MetricsSample>(_count);
        lock (_lock)
        {
            int start = (_next - _count + HistorySize) % HistorySize;
            for (int i = 0; i < _count; i++) result.Add(_history[(start + i) % HistorySize]);
        }
        return result;
    }

    public static double CpuPercent(TimeSpan cpuDelta, double wallSeconds, int processors)
    {
        if (wallSeconds <= 0 || processors <= 0) return 0;
        return Math.Clamp(cpuDelta.TotalSeconds / (wallSeconds * processors) * 100.0, 0, 100);
    }

    private void Sample()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            var cpu = process.TotalProcessorTime;
            double wall = _wall.Elapsed.TotalSeconds;

            var sample = new MetricsSample(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Math.Round(CpuPercent(cpu - _lastCpu, wall - _lastWall, Environment.ProcessorCount), 1),
                Math.Round(process.WorkingSet64 / 1048576.0, 1),
                Math.Round(GC.GetTotalMemory(false) / 1048576.0, 1),
                Time.TPS,
                Math.Round(Time.AverageUpdateTime, 2),
                _players());

            _lastCpu = cpu;
            _lastWall = wall;

            lock (_lock)
            {
                _history[_next] = sample;
                _next = (_next + 1) % HistorySize;
                if (_count < HistorySize) _count++;
            }
        }
        catch (Exception)
        {
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
