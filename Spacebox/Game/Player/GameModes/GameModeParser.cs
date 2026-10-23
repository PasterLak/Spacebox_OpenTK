namespace Spacebox.Game.Player.GameModes;

public static class GameModeParser
{
    private static readonly (System.Reflection.PropertyInfo Override, System.Reflection.PropertyInfo Rule)[] RuleProperties = typeof(GameModeRulesJSON).GetProperties()
        .Select(p => (p, typeof(GameModeRules).GetProperty(p.Name)))
        .Where(pair => pair.Item2 != null && pair.Item2.PropertyType == typeof(bool))
        .ToArray();

    private static void ApplyOverrides(GameModeRules rules, GameModeRulesJSON overrides)
    {
        foreach (var (overrideProperty, ruleProperty) in RuleProperties)
        {
            if (overrideProperty.GetValue(overrides) is bool value) ruleProperty.SetValue(rules, value);
        }
    }

    public static bool TryConvert(GameModeJSON json, out GameModeDefinition mode, out string error)
    {
        mode = null;
        error = null;
        if (json == null || json.Id < 0 || json.Id >= GameModeCatalog.MaxModes)
        {
            error = $"Id must be 0-{GameModeCatalog.MaxModes - 1}";
            return false;
        }

        var id = (GameMode)json.Id;
        GameMode baseMode;
        if (json.Id <= (int)GameMode.Spectator) baseMode = id;
        else if (!Enum.TryParse(json.Base?.Trim(), true, out baseMode) || !Enum.IsDefined(baseMode))
        {
            error = $"Unknown base '{json.Base}' (survival, creative or spectator)";
            return false;
        }

        var rules = GameModeRules.ForBase(baseMode);
        ApplyOverrides(rules, json.Rules ?? new GameModeRulesJSON());

        string key = string.IsNullOrWhiteSpace(json.Key) ? id.ToString().ToLowerInvariant() : json.Key.Trim().ToLowerInvariant();
        string name = string.IsNullOrWhiteSpace(json.Name) ? key : json.Name.Trim();
        mode = new GameModeDefinition(id, key, name, baseMode, rules, GameModeTags.Clamp(json.Tag), json.TagColor);
        return true;
    }
}
