namespace Spacebox.Game.Resource;

public class GeneratorBlockData : BlockData, IExplosiveBlockData, IBlockTooltip
{
    public const ushort DefaultPower = 50;

    public ushort Power { get; }
    public ExplosiveBlockData Explosion { get; }

    public GeneratorBlockData(BlockData baseBlock, PowerGeneratorJSON settings, ExplosiveJSON explosive = null) : base(baseBlock)
    {
        Power = (ushort)Math.Clamp(settings?.Power ?? DefaultPower, 0, 1600);
        Explosion = explosive != null ? new ExplosiveBlockData(baseBlock, explosive) : null;
    }

    public void AppendTooltip(System.Text.StringBuilder text)
    {
        if (Explosion != null) text.Append($"\nExplodes when destroyed: radius {Explosion.Radius:0.#}, penetration {Explosion.Penetration}");
    }
}
