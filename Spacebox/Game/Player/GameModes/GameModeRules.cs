namespace Spacebox.Game.Player.GameModes;

public sealed class GameModeRules
{
    public bool BreakBlocks { get; set; } = true;
    public bool PlaceBlocks { get; set; } = true;
    public bool UseWeapons { get; set; } = true;
    public bool TakeDamage { get; set; } = true;
    public bool NeedOxygen { get; set; } = true;
    public bool Collide { get; set; } = true;
    public bool UseGravity { get; set; } = true;
    public bool MobsAttack { get; set; } = true;
    public bool ConsumeItems { get; set; } = true;
    public bool DropItemsOnDeath { get; set; } = true;
    public bool UseItems { get; set; } = true;
    public bool CreativeInventory { get; set; }
    public bool InstantBreak { get; set; }
    public bool BreakUnbreakable { get; set; }
    public bool AreaTools { get; set; }
    public bool ShipLibrary { get; set; }
    public bool FreeFlashlight { get; set; }
    public bool FullRadar { get; set; }
    public bool CountStatistics { get; set; } = true;
    public bool StartItems { get; set; } = true;

    public static GameModeRules ForBase(GameMode mode)
    {
        return mode switch
        {
            GameMode.Survival => new GameModeRules(),
            GameMode.Creative => new GameModeRules
            {
                TakeDamage = false,
                NeedOxygen = false,
                MobsAttack = false,
                ConsumeItems = false,
                DropItemsOnDeath = false,
                CreativeInventory = true,
                InstantBreak = true,
                BreakUnbreakable = true,
                AreaTools = true,
                ShipLibrary = true
            },
            _ => new GameModeRules
            {
                BreakBlocks = false,
                PlaceBlocks = false,
                UseWeapons = false,
                TakeDamage = false,
                NeedOxygen = false,
                Collide = false,
                UseGravity = false,
                MobsAttack = false,
                ConsumeItems = false,
                DropItemsOnDeath = false,
                UseItems = false,
                FreeFlashlight = true,
                FullRadar = true,
                CountStatistics = false,
                StartItems = false
            }
        };
    }

    public GameModeRules Clone() => (GameModeRules)MemberwiseClone();
}
