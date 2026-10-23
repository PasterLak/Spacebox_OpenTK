namespace Spacebox.Game.Player.GameModes;

public sealed class GameModeJSON
{
    public int Id { get; set; } = -1;
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Base { get; set; } = "survival";
    public string Tag { get; set; } = "";
    public string TagColor { get; set; } = "";
    public GameModeRulesJSON Rules { get; set; } = new GameModeRulesJSON();
}

public sealed class GameModeRulesJSON
{
    public bool? BreakBlocks { get; set; }
    public bool? PlaceBlocks { get; set; }
    public bool? UseWeapons { get; set; }
    public bool? TakeDamage { get; set; }
    public bool? NeedOxygen { get; set; }
    public bool? Collide { get; set; }
    public bool? UseGravity { get; set; }
    public bool? MobsAttack { get; set; }
    public bool? ConsumeItems { get; set; }
    public bool? DropItemsOnDeath { get; set; }
    public bool? UseItems { get; set; }
    public bool? CreativeInventory { get; set; }
    public bool? InstantBreak { get; set; }
    public bool? BreakUnbreakable { get; set; }
    public bool? AreaTools { get; set; }
    public bool? ShipLibrary { get; set; }
    public bool? FreeFlashlight { get; set; }
    public bool? FullRadar { get; set; }
    public bool? CountStatistics { get; set; }
    public bool? StartItems { get; set; }
}
