namespace Spacebox.Game.Resource;

public class OxygenGeneratorBlockData : BlockData
{
    public string WaterItemId { get; }
    public int TankCapacity { get; }
    public float WaterConsumeInterval { get; }
    public float WaterFillInterval { get; }

    private Item _waterItem;

    public OxygenGeneratorBlockData(BlockData baseBlock, OxygenGeneratorJSON settings, string modId) : base(baseBlock)
    {
        settings ??= new OxygenGeneratorJSON();

        WaterItemId = settings.WaterItem.Contains(':') ? settings.WaterItem : GameSetLoader.CombineId(modId, settings.WaterItem);
        TankCapacity = Math.Max(1, settings.TankCapacity);
        WaterConsumeInterval = Math.Max(0.1f, settings.WaterConsumeInterval);
        WaterFillInterval = Math.Max(0.05f, settings.WaterFillInterval);
    }

    public Item WaterItem
    {
        get
        {
            if (_waterItem == null) GameAssets.TryGetItemByFullID(WaterItemId, out _waterItem);
            return _waterItem;
        }
    }
}
