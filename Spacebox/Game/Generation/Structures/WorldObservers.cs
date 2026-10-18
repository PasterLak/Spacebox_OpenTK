using OpenTK.Mathematics;

namespace Spacebox.Game.Generation;

public static class WorldObservers
{
    public const float RemoteEntityRadius = 300f;
    public const float RemoteChunkRadius = 128f;

    private static readonly List<Vector3> _remote = new List<Vector3>();

    public static IReadOnlyList<Vector3> Remote => _remote;

    public static void SetRemote(IEnumerable<Vector3> positions)
    {
        _remote.Clear();
        _remote.AddRange(positions);
    }

    public static void Clear()
    {
        _remote.Clear();
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
}
