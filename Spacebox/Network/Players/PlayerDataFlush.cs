namespace Spacebox.Network.Players;

public sealed class PlayerDataFlush
{
    public const float DefaultTimeout = 2.5f;

    private readonly HashSet<int> _pending = new HashSet<int>();
    private readonly float _timeout;
    private Action _completed;
    private float _timeLeft;

    public PlayerDataFlush(float timeout = DefaultTimeout)
    {
        _timeout = timeout;
    }

    public bool IsRunning => _completed != null;
    public int PendingCount => _pending.Count;

    public void Start(IEnumerable<int> peers, Action requestAll, Action completed)
    {
        Finish();
        _pending.Clear();
        foreach (var peer in peers) _pending.Add(peer);
        if (_pending.Count == 0)
        {
            completed?.Invoke();
            return;
        }

        _completed = completed ?? (() => { });
        _timeLeft = _timeout;
        requestAll();
    }

    public void Received(int peer)
    {
        if (!IsRunning || !_pending.Remove(peer)) return;
        if (_pending.Count == 0) Finish();
    }

    public bool Update(float deltaTime)
    {
        if (!IsRunning) return false;

        _timeLeft -= deltaTime;
        if (_timeLeft > 0f) return false;

        Finish();
        return true;
    }

    private void Finish()
    {
        var completed = _completed;
        _completed = null;
        completed?.Invoke();
    }
}
