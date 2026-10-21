namespace Spacebox.Network.Players;

public sealed class GiftLedger
{
    public readonly struct Gift
    {
        public readonly int Id;
        public readonly int GiverPeer;
        public readonly int TargetPeer;
        public readonly int RequestId;
        public readonly double Deadline;

        public Gift(int id, int giverPeer, int targetPeer, int requestId, double deadline)
        {
            Id = id;
            GiverPeer = giverPeer;
            TargetPeer = targetPeer;
            RequestId = requestId;
            Deadline = deadline;
        }
    }

    private readonly Dictionary<int, Gift> _gifts = new Dictionary<int, Gift>();
    private readonly List<int> _buffer = new List<int>();
    private int _nextId = 1;

    public int Count => _gifts.Count;

    public int Begin(int giverPeer, int targetPeer, int requestId, double deadline)
    {
        int id = _nextId++;
        if (_nextId == int.MaxValue) _nextId = 1;
        _gifts[id] = new Gift(id, giverPeer, targetPeer, requestId, deadline);
        return id;
    }

    public bool TryComplete(int id, int reporterPeer, out Gift gift)
    {
        if (!_gifts.TryGetValue(id, out gift) || gift.TargetPeer != reporterPeer) return false;
        _gifts.Remove(id);
        return true;
    }

    public void TakeExpired(double now, List<Gift> output)
    {
        _buffer.Clear();
        foreach (var pair in _gifts)
            if (now >= pair.Value.Deadline) _buffer.Add(pair.Key);
        Take(output);
    }

    public void TakeForPeer(int peer, List<Gift> output)
    {
        _buffer.Clear();
        foreach (var pair in _gifts)
            if (pair.Value.GiverPeer == peer || pair.Value.TargetPeer == peer) _buffer.Add(pair.Key);
        Take(output);
    }

    public void Clear() => _gifts.Clear();

    private void Take(List<Gift> output)
    {
        for (int i = 0; i < _buffer.Count; i++)
        {
            output.Add(_gifts[_buffer[i]]);
            _gifts.Remove(_buffer[i]);
        }
    }
}
