using Engine;

namespace Spacebox.Network.Dashboard;

public readonly record struct DashboardLogLine(long Id, string Time, string Level, string Text);

public sealed class DashboardLog
{
    public const int Capacity = 1000;
    public const int MaxBatch = 500;

    private readonly DashboardLogLine[] _lines = new DashboardLogLine[Capacity];
    private readonly object _lock = new object();
    private long _nextId = 1;
    private int _count;

    public long LastId
    {
        get
        {
            lock (_lock) return _nextId - 1;
        }
    }

    public void Append(string text, Debug.MessageType type)
    {
        Append(text, type switch
        {
            Debug.MessageType.Error => "error",
            Debug.MessageType.Warning => "warning",
            Debug.MessageType.Success => "success",
            _ => "info"
        });
    }

    public void Append(string text, string level)
    {
        lock (_lock)
        {
            long id = _nextId++;
            _lines[(int)(id % Capacity)] = new DashboardLogLine(id, DateTime.Now.ToString("HH:mm:ss"), level, text ?? "");
            if (_count < Capacity) _count++;
        }
    }

    public List<DashboardLogLine> After(long id)
    {
        var result = new List<DashboardLogLine>();
        lock (_lock)
        {
            long first = Math.Max(id + 1, _nextId - _count);
            if (_nextId - first > MaxBatch) first = _nextId - MaxBatch;
            for (long i = first; i < _nextId; i++) result.Add(_lines[(int)(i % Capacity)]);
        }
        return result;
    }
}
