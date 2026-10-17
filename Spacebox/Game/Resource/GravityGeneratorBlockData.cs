namespace Spacebox.Game.Resource;

public class GravityGeneratorJSON
{
    public float Radius { get; set; } = 32f;
    public float Strength { get; set; } = 9.81f;
    public int Power { get; set; } = 200;
}

public class GravityGeneratorBlockData : BlockData
{
    public float Radius { get; }
    public float Strength { get; }
    public ushort Power { get; }

    public GravityGeneratorBlockData(BlockData baseBlock, GravityGeneratorJSON settings) : base(baseBlock)
    {
        settings ??= new GravityGeneratorJSON();
        Radius = Math.Clamp(settings.Radius, 1f, 256f);
        Strength = Math.Clamp(settings.Strength, 0f, 50f);
        Power = (ushort)Math.Clamp(settings.Power, 0, ushort.MaxValue);
    }
}
