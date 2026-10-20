namespace Spacebox.Game.Resource;

public class DisassemblyJSON
{
    public int DefaultReturnPercent { get; set; } = 50;
    public float DefaultDurationSec { get; set; } = 10f;
    public DisassemblyItemJSON[] Items { get; set; } = [];
}

public class DisassemblyItemJSON
{
    public string Item { get; set; } = "";
    public bool Disabled { get; set; }
    public int? ReturnPercent { get; set; }
    public float? DurationSec { get; set; }
    public int? InputQuantity { get; set; }
    public DisassemblyProductJSON[] Products { get; set; }
}

public class DisassemblyProductJSON
{
    public string Item { get; set; } = "";
    public int Quantity { get; set; } = 1;
}
