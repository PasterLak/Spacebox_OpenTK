namespace Spacebox.Game.Player.GameModes;

public static class GameModeParser
{
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
        var overrides = json.Rules ?? new GameModeRulesJSON();
        rules.BreakBlocks = overrides.BreakBlocks ?? rules.BreakBlocks;
        rules.PlaceBlocks = overrides.PlaceBlocks ?? rules.PlaceBlocks;
        rules.UseWeapons = overrides.UseWeapons ?? rules.UseWeapons;
        rules.TakeDamage = overrides.TakeDamage ?? rules.TakeDamage;
        rules.NeedOxygen = overrides.NeedOxygen ?? rules.NeedOxygen;

        string key = string.IsNullOrWhiteSpace(json.Key) ? id.ToString().ToLowerInvariant() : json.Key.Trim().ToLowerInvariant();
        string name = string.IsNullOrWhiteSpace(json.Name) ? key : json.Name.Trim();
        mode = new GameModeDefinition(id, key, name, baseMode, rules);
        return true;
    }
}
