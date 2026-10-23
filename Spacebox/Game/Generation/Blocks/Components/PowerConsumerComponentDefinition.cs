namespace Spacebox.Game.Generation.Blocks.Components;

public sealed class PowerConsumerComponentDefinition : BlockComponentDefinition
{
    public const string ComponentName = "consumer";

    public PowerConsumerComponentDefinition(ushort power)
    {
        Power = power;
    }

    public ushort Power { get; }

    public override string Name => ComponentName;
    public override BlockComponent Create() => new PowerConsumerComponent(this);
}
