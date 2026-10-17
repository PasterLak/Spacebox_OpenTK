using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Oxygen;

public class OxygenGeneratorBlock : ElectricalBlock
{
    public const int Radius = 32;
    public const byte RequiredPower = 20;

    public OxygenGeneratorBlock(BlockData blockData) : base(blockData)
    {
        EFlags = ElectricalFlags.CanConsume;
        MaxPower = 300;
        ConsumptionRate = RequiredPower;
    }

    public override void TickElectric()
    {
        bool isPowered = CurrentPower >= ConsumptionRate;
        base.TickElectric();
        IsActive = isPowered;
    }
}
