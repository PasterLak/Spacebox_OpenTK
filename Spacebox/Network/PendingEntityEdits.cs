namespace Spacebox.Network;

public sealed class PendingEntityEdits<T>
{
    public const float DefaultTimeout = 15f;
    public const int DefaultMaxPerPeer = 256;

    private readonly List<Entry> _entries = new List<Entry>();
    private readonly List<Entry> _ready = new List<Entry>();
    private readonly float _timeout;
    private readonly int _maxPerPeer;

    public PendingEntityEdits(float timeout = DefaultTimeout, int maxPerPeer = DefaultMaxPerPeer)
    {
        _timeout = timeout;
        _maxPerPeer = maxPerPeer;
    }

    public int Count => _entries.Count;

    public bool Has(long entityId)
    {
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].EntityId == entityId) return true;
        return false;
    }

    public bool TryAdd(int peer, long entityId, T message)
    {
        int count = 0;
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].Peer == peer) count++;
        if (count >= _maxPerPeer) return false;

        _entries.Add(new Entry(peer, entityId, message, _timeout));
        return true;
    }

    public void Update(float deltaTime, Func<long, bool> isKnown, Action<int, T> replay, Action<int, T> expire)
    {
        if (_entries.Count == 0) return;

        _ready.Clear();
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            entry.TimeLeft -= deltaTime;
            if (isKnown(entry.EntityId) || entry.TimeLeft <= 0f) _ready.Add(entry);
        }

        if (_ready.Count == 0) return;

        _entries.RemoveAll(entry => _ready.Contains(entry));
        foreach (var entry in _ready)
        {
            if (isKnown(entry.EntityId)) replay(entry.Peer, entry.Message);
            else expire(entry.Peer, entry.Message);
        }
        _ready.Clear();
    }

    public List<T> RemovePeer(int peer)
    {
        var removed = new List<T>();
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].Peer != peer) continue;
            removed.Add(_entries[i].Message);
            _entries.RemoveAt(i);
        }
        return removed;
    }

    public void Clear()
    {
        _entries.Clear();
        _ready.Clear();
    }

    private sealed class Entry
    {
        public Entry(int peer, long entityId, T message, float timeLeft)
        {
            Peer = peer;
            EntityId = entityId;
            Message = message;
            TimeLeft = timeLeft;
        }

        public int Peer { get; }
        public long EntityId { get; }
        public T Message { get; }
        public float TimeLeft { get; set; }
    }
}
