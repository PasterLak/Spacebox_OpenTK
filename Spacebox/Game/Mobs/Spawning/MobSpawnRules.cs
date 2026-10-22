using OpenTK.Mathematics;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Game.Mobs.Spawning;

public readonly struct MobSpawnLimits
{
    public MobSpawnLimits(int maxPerChunk, int maxPerEntity, float maxLightLevel, byte chance)
    {
        MaxPerChunk = maxPerChunk;
        MaxPerEntity = maxPerEntity;
        MaxLightLevel = maxLightLevel;
        Chance = chance;
    }

    public int MaxPerChunk { get; }
    public int MaxPerEntity { get; }
    public float MaxLightLevel { get; }
    public byte Chance { get; }
}

public sealed class MobSpawnRules
{
    private readonly HashSet<string> _listedMobs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public MobSpawnRules(IEnumerable<AsteroidData> asteroids)
    {
        if (asteroids == null) return;

        foreach (var asteroid in asteroids)
        {
            if (asteroid?.Mobs == null) continue;
            foreach (var rule in asteroid.Mobs) _listedMobs.Add(rule.MobId);
        }
    }

    public bool IsListed(string mobId) => mobId != null && _listedMobs.Contains(mobId);

    public static AsteroidMobRule FindRule(AsteroidData asteroid, string mobId, string biomeId, float distanceFromCenter)
    {
        if (asteroid?.Mobs == null) return null;

        foreach (var rule in asteroid.Mobs)
        {
            if (!string.Equals(rule.MobId, mobId, StringComparison.OrdinalIgnoreCase)) continue;
            if (!rule.AllowsBiome(biomeId) || !rule.Distance.Contains(distanceFromCenter)) continue;
            return rule;
        }

        return null;
    }

    public static MobSpawnLimits Limits(MobSpawnJSON defaults, AsteroidMobRule rule)
    {
        if (rule == null) return new MobSpawnLimits(defaults.MaxPerChunk, 0, defaults.MaxLightLevel, 100);

        return new MobSpawnLimits(
            rule.MaxPerChunk ?? defaults.MaxPerChunk,
            rule.MaxPerEntity,
            rule.MaxLightLevel ?? defaults.MaxLightLevel,
            rule.Chance);
    }

    public static bool AllowsCell(AsteroidMobRule rule, AsteroidData asteroid, float depth)
    {
        if (rule == null) return true;

        if (depth < rule.MinDepth || depth > rule.MaxDepth) return false;
        if (rule.Layers.Length == 0) return true;

        return rule.AllowsLayer(AsteroidLayers.IndexAt(asteroid.LayerThresholds, depth));
    }
}
