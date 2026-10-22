namespace Spacebox.Network;

public sealed class AdmissionWatch
{
    public const float JoinRequestTimeout = 30f;
    public const float AdmissionTimeout = 200f;

    private readonly Dictionary<int, Pending> _pending = new Dictionary<int, Pending>();
    private readonly List<int> _expired = new List<int>();

    public int Count => _pending.Count;

    public void Connected(int peer) => _pending[peer] = new Pending();

    public void Requested(int peer)
    {
        if (_pending.TryGetValue(peer, out var pending)) pending.Requested = true;
    }

    public void Forget(int peer) => _pending.Remove(peer);

    public IReadOnlyList<int> Update(float deltaTime, Func<int, bool> isAdmitted)
    {
        _expired.Clear();
        foreach (var pair in _pending)
        {
            var pending = pair.Value;
            pending.Age += deltaTime;
            if (isAdmitted(pair.Key) || pending.Age >= (pending.Requested ? AdmissionTimeout : JoinRequestTimeout)) _expired.Add(pair.Key);
        }

        for (int i = _expired.Count - 1; i >= 0; i--)
        {
            int peer = _expired[i];
            _pending.Remove(peer);
            if (isAdmitted(peer)) _expired.RemoveAt(i);
        }
        return _expired;
    }

    public void Clear() => _pending.Clear();

    private sealed class Pending
    {
        public float Age;
        public bool Requested;
    }
}
