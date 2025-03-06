
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation
{
    public class GeneratorBlock : ElectricalBlock
    {
        public GeneratorBlock(BlockData blockData) : base(blockData)
        {
            EFlags = ElectricalFlags.CanGenerate | ElectricalFlags.CanTransfer;
            MaxPower = 500;
            GenerationRate = 50;
            EnableEmission = true;
        }
        public override void TickElectric()
        {
            base.TickElectric();
            SetEnableEmission(CurrentPower > 0);
        }


    }
}
