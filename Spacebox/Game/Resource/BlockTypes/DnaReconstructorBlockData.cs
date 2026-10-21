namespace Spacebox.Game.Resource;

public class DnaReconstructorBlockData : BlockData, IBlockTooltip
{
    public string DnaItemId { get; }
    public ushort Power { get; }
    public float ReconstructionTime { get; }
    public DnaReconstructorSoundsJSON Sounds { get; }

    private Item _dnaItem;

    public DnaReconstructorBlockData(BlockData baseBlock, DnaReconstructorJSON settings, string modId) : base(baseBlock)
    {
        settings ??= new DnaReconstructorJSON();

        string dnaItem = string.IsNullOrEmpty(settings.DnaItem) ? "dna_ui" : settings.DnaItem;
        DnaItemId = dnaItem.Contains(':') ? dnaItem : GameSetLoader.CombineId(modId, dnaItem);
        Power = (ushort)Math.Clamp(settings.Power, 0, 8000);
        ReconstructionTime = Math.Clamp(settings.ReconstructionTime, 1f, 30f);
        Sounds = settings.Sounds ?? new DnaReconstructorSoundsJSON();
    }

    public void AppendTooltip(System.Text.StringBuilder text)
    {
        text.Append($"\nUses 1 {DnaItem?.Name ?? "DNA sample"} per revival");
        text.Append($"\nRevival time: {ReconstructionTime:0.#} s");
        text.Append($"\nPower usage: {Power} EU");
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
