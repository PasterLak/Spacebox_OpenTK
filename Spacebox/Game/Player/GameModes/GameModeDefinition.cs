namespace Spacebox.Game.Player.GameModes;

public sealed class GameModeDefinition
{
    public GameModeDefinition(GameMode id, string key, string name, GameMode baseMode, GameModeRules rules, string tag = null, string tagColor = null)
    {
        Tag = string.IsNullOrWhiteSpace(tag) ? GameModeTags.DefaultTag(name) : tag.Trim();
        TagColor = string.IsNullOrWhiteSpace(tagColor) ? GameModeTags.DefaultColor(baseMode) : tagColor.Trim();
        Id = id;
        Key = key;
        Name = name;
        Base = baseMode;
        Rules = rules;
    }

    public GameMode Id { get; }
    public string Key { get; }
    public string Name { get; }
    public GameMode Base { get; }
    public GameModeRules Rules { get; }
    public string Tag { get; }
    public string TagColor { get; }

    public static GameModeDefinition BuiltIn(GameMode mode)
    {
        string key = mode.ToString().ToLowerInvariant();
        return new GameModeDefinition(mode, key, mode.ToString(), mode, GameModeRules.ForBase(mode));
    }
}
