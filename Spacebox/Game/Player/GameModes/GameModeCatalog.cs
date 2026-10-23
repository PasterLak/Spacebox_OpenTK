namespace Spacebox.Game.Player.GameModes;

public static class GameModeCatalog
{
    public const int MaxModes = 32;

    private static readonly List<GameModeDefinition> _modes = new List<GameModeDefinition>();

    static GameModeCatalog()
    {
        Reset();
    }

    public static IReadOnlyList<GameModeDefinition> All => _modes;

    public static void Reset() => Set(Array.Empty<GameModeDefinition>());

    public static void Set(IEnumerable<GameModeDefinition> custom)
    {
        _modes.Clear();
        _modes.Add(GameModeDefinition.BuiltIn(GameMode.Survival));
        _modes.Add(GameModeDefinition.BuiltIn(GameMode.Creative));
        _modes.Add(GameModeDefinition.BuiltIn(GameMode.Spectator));

        foreach (var mode in custom)
        {
            int index = _modes.FindIndex(existing => existing.Id == mode.Id);
            if (index >= 0) _modes[index] = mode;
            else if ((int)mode.Id < MaxModes) _modes.Add(mode);
        }
        _modes.Sort((a, b) => ((int)a.Id).CompareTo((int)b.Id));
    }

    public static bool Contains(GameMode id) => _modes.Exists(mode => mode.Id == id);

    public static GameModeDefinition Get(GameMode id) => _modes.Find(mode => mode.Id == id) ?? _modes[0];

    public static GameMode BaseOf(GameMode id) => Get(id).Base;

    public static GameModeRules RulesOf(GameMode id) => Get(id).Rules;

    public static string NameOf(GameMode id) => Contains(id) ? Get(id).Name : "Unknown";

    public static bool TryFind(string text, out GameModeDefinition mode)
    {
        mode = null;
        if (string.IsNullOrWhiteSpace(text)) return false;

        string value = text.Trim();
        if (int.TryParse(value, out int number))
        {
            mode = _modes.Find(candidate => (int)candidate.Id == number);
            return mode != null;
        }

        mode = _modes.Find(candidate => string.Equals(candidate.Key, value, StringComparison.OrdinalIgnoreCase) || string.Equals(candidate.Name, value, StringComparison.OrdinalIgnoreCase));
        return mode != null;
    }
}
