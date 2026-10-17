namespace Spacebox.Game.Resource;

public class DnaReconstructorBlockData : BlockData
{
    public string DnaItemId { get; }
    public ushort Power { get; }
    public float ReconstructionTime { get; }

    private Item _dnaItem;

    public DnaReconstructorBlockData(BlockData baseBlock, DnaReconstructorJSON settings, string modId) : base(baseBlock)
    {
        settings ??= new DnaReconstructorJSON();

        string dnaItem = string.IsNullOrEmpty(settings.DnaItem) ? "dna_sample" : settings.DnaItem;
        DnaItemId = dnaItem.Contains(':') ? dnaItem : GameSetLoader.CombineId(modId, dnaItem);
        Power = (ushort)Math.Clamp(settings.Power, 0, 8000);
        ReconstructionTime = Math.Clamp(settings.ReconstructionTime, 1f, 30f);
    }

    public Item DnaItem
    {
        get
        {
            if (_dnaItem == null) GameAssets.TryGetItemByFullID(DnaItemId, out _dnaItem);
            return _dnaItem;
        }
    }
}
