using Engine;

namespace Spacebox.Agent;

public sealed record AgentLogEntry(long Seq, string Type, string Text, string Time);

public sealed class AgentLog
{
    private const int Capacity = 2000;

    private readonly object _lock = new object();
    private readonly Queue<AgentLogEntry> _entries = new Queue<AgentLogEntry>();
    private long _seq;

    public long LastSeq
    {
        get
        {
            lock (_lock) return _seq;
        }
    }

    public void Attach() => Debug.MessageLogged += OnMessage;

    public void Detach() => Debug.MessageLogged -= OnMessage;

    public List<AgentLogEntry> Since(long seq, int limit)
    {
        var result = new List<AgentLogEntry>();
        lock (_lock)
        {
            foreach (var entry in _entries)
            {
                if (entry.Seq > seq) result.Add(entry);
            }
        }

        return result.Count > limit ? result.GetRange(result.Count - limit, limit) : result;
    }

    private void OnMessage(string text, Debug.MessageType type)
    {
        lock (_lock)
        {
            _seq++;
            _entries.Enqueue(new AgentLogEntry(_seq, type.ToString(), text, DateTime.Now.ToString("HH:mm:ss.fff")));
            while (_entries.Count > Capacity) _entries.Dequeue();
        }
    }
}
