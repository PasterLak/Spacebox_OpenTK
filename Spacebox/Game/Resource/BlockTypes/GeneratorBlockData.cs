namespace Spacebox.Game.Resource;

public class GeneratorBlockData : BlockData, IExplosiveBlockData, IBlockTooltip
{
    public const ushort DefaultPower = 50;
    public const int MaxPower = 1600;

    public ushort Power { get; }
    public ExplosiveBlockData Explosion { get; }

    public GeneratorBlockData(BlockData baseBlock, PowerGeneratorJSON settings, ExplosiveJSON explosive = null) : base(baseBlock)
    {
        Power = ClampPower(settings?.Power ?? DefaultPower);
        Explosion = explosive != null ? new ExplosiveBlockData(baseBlock, explosive) : null;
    }

    public static ushort ClampPower(int power) => (ushort)Math.Clamp(power, 0, MaxPower);

    public void AppendTooltip(System.Text.StringBuilder text)
    {
        if (Explosion != null) text.Append($"\nExplodes when destroyed: radius {Explosion.Radius:0.#}, penetration {Explosion.Penetration}");
    }
}
