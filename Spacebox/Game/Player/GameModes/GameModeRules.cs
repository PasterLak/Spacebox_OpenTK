namespace Spacebox.Game.Player.GameModes;

public sealed class GameModeRules
{
    public bool BreakBlocks { get; set; } = true;
    public bool PlaceBlocks { get; set; } = true;
    public bool UseWeapons { get; set; } = true;
    public bool TakeDamage { get; set; } = true;
    public bool NeedOxygen { get; set; } = true;

    public static GameModeRules ForBase(GameMode mode)
    {
        return mode switch
        {
            GameMode.Survival => new GameModeRules(),
            GameMode.Creative => new GameModeRules { TakeDamage = false, NeedOxygen = false },
            _ => new GameModeRules { BreakBlocks = false, PlaceBlocks = false, UseWeapons = false, TakeDamage = false, NeedOxygen = false }
        };
    }

    public GameModeRules Clone() => (GameModeRules)MemberwiseClone();
}
