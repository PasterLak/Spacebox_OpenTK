namespace Spacebox.Game.Generation.Blocks.Components;

public sealed class PowerGeneratorComponentDefinition : BlockComponentDefinition
{
    public const string ComponentName = "generator";

    public PowerGeneratorComponentDefinition(ushort power)
    {
        Power = power;
    }

    public ushort Power { get; }

    public override string Name => ComponentName;
    public override BlockComponent Create() => new PowerGeneratorComponent(this);
}
