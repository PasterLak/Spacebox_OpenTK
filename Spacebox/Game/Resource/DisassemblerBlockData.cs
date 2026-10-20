namespace Spacebox.Game.Resource;

public class DisassemblerBlockData : BlockData, IBlockTooltip
{
    public ushort Power { get; }

    public DisassemblerBlockData(BlockData baseBlock, DisassemblerJSON settings) : base(baseBlock)
    {
        settings ??= new DisassemblerJSON();
        Power = (ushort)Math.Clamp(settings.Power, 0, 8000);
    }

    public void AppendTooltip(System.Text.StringBuilder text)
    {
        text.Append($"\nPower usage: {Power} EU");
    }
}
