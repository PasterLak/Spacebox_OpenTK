using OpenTK.Mathematics;

namespace Spacebox.Game.Generation;

public static class WorldObservers
{
    public const float RemoteEntityRadius = 300f;
    public const float RemoteChunkRadius = 128f;

    private struct Anchor
    {
        public Vector3 Position;
        public float TimeLeft;
    }

    private static readonly List<Vector3> _remote = new List<Vector3>();
    private static readonly List<Vector3> _players = new List<Vector3>();
    private static readonly List<Anchor> _anchors = new List<Anchor>();

    public static IReadOnlyList<Vector3> Remote => _remote;

    public static void SetRemote(IEnumerable<Vector3> positions)
    {
        _players.Clear();
        _players.AddRange(positions);
        Rebuild();
    }

    public static void AddAnchor(Vector3 position, float seconds)
    {
        if (seconds <= 0f) return;

        _anchors.Add(new Anchor { Position = position, TimeLeft = seconds });
        Rebuild();
    }

    public static void Update(float deltaTime)
    {
        if (_anchors.Count == 0) return;

        bool changed = false;
        for (int i = _anchors.Count - 1; i >= 0; i--)
        {
            var anchor = _anchors[i];
            anchor.TimeLeft -= deltaTime;
            if (anchor.TimeLeft > 0f)
            {
                _anchors[i] = anchor;
                continue;
            }

            _anchors.RemoveAt(i);
            changed = true;
        }

        if (changed) Rebuild();
    }

    public static void Clear()
    {
        _remote.Clear();
        _players.Clear();
        _anchors.Clear();
    }

    public static bool IsNearRemote(Vector3 position, float distance)
    {
        float distanceSquared = distance * distance;
        for (int i = 0; i < _remote.Count; i++)
        {
            if (Vector3.DistanceSquared(_remote[i], position) <= distanceSquared) return true;
        }
        return false;
    }

    private static void Rebuild()
    {
        _remote.Clear();
        _remote.AddRange(_players);
        for (int i = 0; i < _anchors.Count; i++) _remote.Add(_anchors[i].Position);
    }
}
