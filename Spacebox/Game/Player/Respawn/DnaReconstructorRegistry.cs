using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Player.Respawn;

public static class DnaReconstructorRegistry
{
    private static readonly List<DnaReconstructorBlock> _blocks = new List<DnaReconstructorBlock>();
    private static readonly object _lock = new object();

    public static void Register(DnaReconstructorBlock block)
    {
        lock (_lock)
        {
            if (!_blocks.Contains(block)) _blocks.Add(block);
        }
    }

    public static void Unregister(DnaReconstructorBlock block)
    {
        lock (_lock) _blocks.Remove(block);
    }

    public static void UnregisterEntity(SpaceEntity entity)
    {
        lock (_lock) _blocks.RemoveAll(block => block.Entity == entity);
    }

    public static void Clear()
    {
        lock (_lock) _blocks.Clear();
    }

    public static bool HasAny()
    {
        lock (_lock) return _blocks.Count > 0;
    }

    public static bool HasAnyWithDna()
    {
        lock (_lock)
        {
            for (int i = 0; i < _blocks.Count; i++)
            {
                if (_blocks[i].HasDna) return true;
            }
        }
        return false;
    }

    public static bool TryFindNearestReady(Vector3 worldPosition, out DnaReconstructorBlock nearest)
    {
        nearest = null;
        float nearestDistance = float.MaxValue;

        lock (_lock)
        {
            for (int i = 0; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                if (!block.IsReady) continue;

                float distance = Vector3.DistanceSquared(worldPosition, block.WorldCenter);
                if (distance >= nearestDistance) continue;

                nearestDistance = distance;
                nearest = block;
            }
        }

        return nearest != null;
    }
}
