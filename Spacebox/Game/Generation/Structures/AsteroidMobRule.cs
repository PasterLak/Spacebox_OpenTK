namespace Spacebox.Game.Generation.Structures;

public sealed class AsteroidMobRule
{
    public string MobId { get; init; } = "";
    public byte Chance { get; init; } = 100;
    public int? MaxPerChunk { get; init; }
    public int MaxPerEntity { get; init; }
    public int[] Layers { get; init; } = Array.Empty<int>();
    public float MinDepth { get; init; }
    public float MaxDepth { get; init; } = 1f;
    public float? MaxLightLevel { get; init; }
    public HashSet<string> Biomes { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public DistanceRange Distance { get; init; } = DistanceRange.Everywhere;

    public static AsteroidMobRule FromJson(AsteroidMobJSON json, string mobId)
    {
        return new AsteroidMobRule
        {
            MobId = mobId,
            Chance = json.Chance,
            MaxPerChunk = json.MaxPerChunk,
            MaxPerEntity = Math.Max(0, json.MaxPerEntity),
            Layers = json.Layers ?? Array.Empty<int>(),
            MinDepth = Math.Clamp(Math.Min(json.MinDepth, json.MaxDepth), 0f, 1f),
            MaxDepth = Math.Clamp(Math.Max(json.MinDepth, json.MaxDepth), 0f, 1f),
            MaxLightLevel = json.MaxLightLevel,
            Biomes = new HashSet<string>(json.Biomes ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase),
            Distance = new DistanceRange(json.MinDistanceFromCenter, json.MaxDistanceFromCenter)
        };
    }

    public bool AllowsLayer(int layer)
    {
        if (Layers.Length == 0) return true;
        for (int i = 0; i < Layers.Length; i++)
        {
            if (Layers[i] == layer) return true;
        }
        return false;
    }

    public bool AllowsBiome(string biomeId)
    {
        return Biomes.Count == 0 || (biomeId != null && Biomes.Contains(biomeId));
    }
}
