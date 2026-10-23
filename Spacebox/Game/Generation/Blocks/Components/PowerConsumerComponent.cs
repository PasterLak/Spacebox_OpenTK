namespace Spacebox.Game.Generation.Blocks.Components;

public sealed class PowerConsumerComponent : BlockComponent
{
    private readonly ushort _power;

    public PowerConsumerComponent(PowerConsumerComponentDefinition settings)
    {
        _power = settings.Power;
    }

    public override bool ConsumesPower => true;

    protected override void OnAttached()
    {
        Host.EFlags |= ElectricalFlags.CanConsume;
        Host.ConsumptionRate = (ushort)Math.Min(Host.ConsumptionRate + _power, ushort.MaxValue);
    }
}
