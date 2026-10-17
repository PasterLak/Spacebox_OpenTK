namespace Spacebox.Game.Resource;

public class GeneratorBlockData : BlockData
{
    public const ushort DefaultPower = 50;

    public ushort Power { get; }

    public GeneratorBlockData(BlockData baseBlock, PowerGeneratorJSON settings) : base(baseBlock)
    {
        Power = (ushort)Math.Clamp(settings?.Power ?? DefaultPower, 0, 1600);
    }
}
