using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Game.Mobs;

public static class NaturalBlocks
{
    private const string VoidId = "default:void";
    private const string AirId = "default:air";

    private static readonly HashSet<short> _ids = new HashSet<short>();
    private static WorldGenerator? _source;

    public static bool Contains(short blockId)
    {
        EnsureBuilt();
        return _ids.Contains(blockId);
    }

    public static void Reset()
    {
        _ids.Clear();
        _source = null;
    }

    private static void EnsureBuilt()
    {
        var generator = World.WorldGenerator;
        if (generator == _source) return;

        _ids.Clear();
        _source = generator;
        if (generator == null) return;

        foreach (var asteroid in generator.loadedAsteroids.Values)
        {
            if (asteroid?.Layers == null) continue;

            foreach (var layer in asteroid.Layers)
            {
                if (layer == null) continue;
                Add(layer.FillBlockID);

                if (layer.Veins == null) continue;
                foreach (var vein in layer.Veins)
                {
                    if (vein != null) Add(vein.BlockId);
                }
            }
        }
    }

    private static void Add(short blockId)
    {
        if (blockId <= 0) return;
        if (!GameAssets.Blocks.TryGetValue(blockId, out var data) || data.IsTransparent) return;
        if (data.Id_string == VoidId || data.Id_string == AirId) return;
        _ids.Add(blockId);
    }
}
