namespace Spacebox.Game.Resource;

public class DisassemblerBlockData : BlockData
{
    public ushort Power { get; }

    public DisassemblerBlockData(BlockData baseBlock, DisassemblerJSON settings) : base(baseBlock)
    {
        settings ??= new DisassemblerJSON();
        Power = (ushort)Math.Clamp(settings.Power, 0, 8000);
    }
}
