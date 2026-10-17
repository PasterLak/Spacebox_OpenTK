using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks
{
    public class CableBlock : ElectricalBlock
    {

        public CableBlock(BlockData blockData) : base(blockData)
        {
            EFlags = ElectricalFlags.CanTransfer | ElectricalFlags.CanConsume;
            MaxPower = 200;
            ConsumptionRate = 1;
        }

        public override void TickElectric()
        {
            base.TickElectric();
            SetEnableEmission(CurrentPower > 0);
        }


    }
}
