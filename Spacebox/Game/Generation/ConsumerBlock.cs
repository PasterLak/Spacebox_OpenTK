
using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
{
    public class ConsumerBlock : ElectricalBlock
    {

        public ConsumerBlock(BlockData blockData) : base(blockData)
        {
            EFlags = ElectricalFlags.CanConsume;
            MaxPower = 300;
            ConsumptionRate = 20;
        }

        public override void TickElectric()
        {
            base.TickElectric();

            SetEnableEmission(CurrentPower > 0);
        }

    }


}