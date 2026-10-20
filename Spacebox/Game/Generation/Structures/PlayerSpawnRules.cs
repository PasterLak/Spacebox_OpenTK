namespace Spacebox.Game.Generation.Structures;

public class PlayerSpawnJSON
{
    public int MinAsteroidRadius { get; set; } = 0;
    public string[] ExcludedAsteroids { get; set; } = new string[] { };
}

public sealed class PlayerSpawnRules
{
    public static readonly PlayerSpawnRules Default = new PlayerSpawnRules(0, null);

    private readonly HashSet<string> _excluded;

    public PlayerSpawnRules(int minAsteroidRadius, IEnumerable<string> excludedAsteroids)
    {
        MinAsteroidRadius = Math.Max(0, minAsteroidRadius);
        _excluded = new HashSet<string>(excludedAsteroids ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
    }

    public int MinAsteroidRadius { get; }
    public IReadOnlyCollection<string> ExcludedAsteroids => _excluded;

    public static PlayerSpawnRules FromJson(PlayerSpawnJSON json)
    {
        return json == null ? Default : new PlayerSpawnRules(json.MinAsteroidRadius, json.ExcludedAsteroids);
    }

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
