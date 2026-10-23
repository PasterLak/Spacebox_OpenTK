namespace Spacebox.Game.Player.GameModes;

public sealed class GameModeJSON
{
    public int Id { get; set; } = -1;
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Base { get; set; } = "survival";
    public GameModeRulesJSON Rules { get; set; } = new GameModeRulesJSON();
}

public sealed class GameModeRulesJSON
{
    public bool? BreakBlocks { get; set; }
    public bool? PlaceBlocks { get; set; }
    public bool? UseWeapons { get; set; }
    public bool? TakeDamage { get; set; }
    public bool? NeedOxygen { get; set; }
}
