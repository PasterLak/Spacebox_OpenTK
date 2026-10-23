using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;

namespace Spacebox.Game.Generation.Blocks.Components;

public sealed class PowerGeneratorComponent : BlockComponent
{
    public const int Priority = 10;

    private readonly ushort _power;

    public PowerGeneratorComponent(PowerGeneratorComponentDefinition settings)
    {
        _power = settings.Power;
    }

    public override int UsePriority => Priority;
    public override bool GeneratesPower => true;

    public static short StoredPowerFor(ushort power) => (short)Math.Clamp(power * 10, 500, 16000);

    protected override void OnAttached()
    {
        Host.EFlags |= ElectricalFlags.CanGenerate | ElectricalFlags.CanTransfer;
        Host.MaxPower = Math.Max(Host.MaxPower, StoredPowerFor(_power));
        Host.GenerationRate = (ushort)Math.Min(Host.GenerationRate + _power, ushort.MaxValue);
        Host.EnableEmission = true;
    }

    public override void TickElectric()
    {
        Host.SetEnableEmission(Host.CurrentPower > 0);
    }

    public override void Use(LocalAstronaut player, ref HitInfo hit)
    {
        var generator = hit.chunk?.SpaceEntity.GetStructureOrigin(hit.chunk, hit.blockPositionIndex) as ElectricalBlock ?? Host;
        GeneratorUI.Open(generator, player, ref hit);
    }
}
