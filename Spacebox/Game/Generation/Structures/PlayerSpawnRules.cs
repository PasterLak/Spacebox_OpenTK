namespace Spacebox.Game.Generation.Structures;

public class PlayerSpawnJSON
{
    public int MinAsteroidRadius { get; set; } = 0;
    public string[] ExcludedAsteroids { get; set; } = new string[] { };
    public string PreferredSpawnBiome { get; set; } = "";
    public int MinDistanceFromCenter { get; set; } = 0;
    public int MaxDistanceFromCenter { get; set; } = 0;
    public int HealthOnSpawn { get; set; } = 100;
    public int PowerOnSpawn { get; set; } = 100;
    public int OxygenOnSpawn { get; set; } = 100;
}

public readonly record struct SpawnCandidate(string AsteroidId, int Radius, string BiomeId, float DistanceFromCenter);

public sealed class PlayerSpawnRules
{
    public static readonly PlayerSpawnRules Default = new PlayerSpawnRules(0, null);
    public static PlayerSpawnRules Current => World.WorldGenerator?.PlayerSpawn ?? Default;

    private readonly HashSet<string> _excluded;

    public PlayerSpawnRules(int minAsteroidRadius, IEnumerable<string> excludedAsteroids)
    {
        MinAsteroidRadius = Math.Max(0, minAsteroidRadius);
        _excluded = new HashSet<string>(excludedAsteroids ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
    }

    public int MinAsteroidRadius { get; }
    public IReadOnlyCollection<string> ExcludedAsteroids => _excluded;
    public string PreferredBiome { get; init; } = "";
    public DistanceRange Distance { get; init; } = DistanceRange.Everywhere;
    public int HealthPercent { get; init; } = 100;
    public int PowerPercent { get; init; } = 100;
    public int OxygenPercent { get; init; } = 100;

    public static PlayerSpawnRules FromJson(PlayerSpawnJSON json)
    {
        if (json == null) return Default;

        return new PlayerSpawnRules(json.MinAsteroidRadius, json.ExcludedAsteroids)
        {
            PreferredBiome = IsNone(json.PreferredSpawnBiome) ? "" : json.PreferredSpawnBiome.Trim(),
            Distance = new DistanceRange(json.MinDistanceFromCenter, json.MaxDistanceFromCenter),
            HealthPercent = Math.Clamp(json.HealthOnSpawn, 1, 100),
            PowerPercent = Math.Clamp(json.PowerOnSpawn, 0, 100),
            OxygenPercent = Math.Clamp(json.OxygenOnSpawn, 0, 100)
        };
    }

    public static int ValueOf(int max, int percent) => Math.Clamp((int)MathF.Round(max * percent / 100f), 0, max);

    public int PickIndex(IReadOnlyList<SpawnCandidate> candidates, Random random)
    {
        if (candidates == null || candidates.Count == 0) return -1;

        var pool = Indices(candidates, c => Distance.Contains(c.DistanceFromCenter) && IsPreferredBiome(c.BiomeId));
        if (pool.Count == 0) pool = Indices(candidates, c => Distance.Contains(c.DistanceFromCenter));
        if (pool.Count == 0) pool = Indices(candidates, _ => true);

        var subset = new List<(string AsteroidId, int Radius)>(pool.Count);
        for (int i = 0; i < pool.Count; i++) subset.Add((candidates[pool[i]].AsteroidId, candidates[pool[i]].Radius));

        int picked = PickIndex(subset, random);
        return picked < 0 ? -1 : pool[picked];
    }

    private bool IsPreferredBiome(string biomeId)
    {
        return string.IsNullOrEmpty(PreferredBiome) || string.Equals(PreferredBiome, biomeId, StringComparison.OrdinalIgnoreCase);
    }

    private static List<int> Indices(IReadOnlyList<SpawnCandidate> candidates, Func<SpawnCandidate, bool> include)
    {
        var result = new List<int>();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (include(candidates[i])) result.Add(i);
        }
        return result;
    }

    private static bool IsNone(string value) => string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "none", StringComparison.OrdinalIgnoreCase);

    public bool IsExcluded(string asteroidId)
    {
        return !string.IsNullOrEmpty(asteroidId) && _excluded.Contains(asteroidId);
    }

    public bool IsAllowed(string asteroidId, int radius)
    {
        return !IsExcluded(asteroidId) && radius >= MinAsteroidRadius;
    }

    public int PickIndex(IReadOnlyList<(string AsteroidId, int Radius)> candidates, Random random)
    {
        if (candidates == null || candidates.Count == 0) return -1;

        int allowed = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (IsAllowed(candidates[i].AsteroidId, candidates[i].Radius)) allowed++;
        }

        if (allowed > 0)
        {
            int target = random.Next(allowed);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (!IsAllowed(candidates[i].AsteroidId, candidates[i].Radius)) continue;
                if (target-- == 0) return i;
            }
        }

        int best = -1;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (best < 0 || IsBetterFallback(candidates[i], candidates[best])) best = i;
        }
        return best;
    }

    private bool IsBetterFallback((string AsteroidId, int Radius) candidate, (string AsteroidId, int Radius) current)
    {
        bool candidateExcluded = IsExcluded(candidate.AsteroidId);
        bool currentExcluded = IsExcluded(current.AsteroidId);
        if (candidateExcluded != currentExcluded) return !candidateExcluded;
        return candidate.Radius > current.Radius;
    }
}
