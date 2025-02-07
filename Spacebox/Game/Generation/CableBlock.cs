

using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
{
    public class CableBlock : ElectricalBlock
    {

        public CableBlock(BlockData blockData) : base(blockData)
        {
            EFlags = ElectricalFlags.CanTransfer | ElectricalFlags.CanConsume;
            MaxPower = 200;
            ConsumptionRate = 2;
        }

        public override void TickElectric()
        {
            base.TickElectric();
            SetEnableEmission(CurrentPower > 0);
        }

           
    }
}
