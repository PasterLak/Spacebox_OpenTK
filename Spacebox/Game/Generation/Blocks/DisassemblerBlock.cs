using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks
{

    public class DisassemblerBlock : ResourceProcessingBlock
    {

        public DisassemblerBlock(BlockData blockData) : base(blockData)
        {
            OnUse += ResourceProcessingGUI.Toggle;

            SetEmissionWithoutRedrawChunk(false);

            EFlags = ElectricalFlags.CanConsume;
      
            ConsumptionRate = 15;
            CurrentPower = 0;
        }

        public override void Use(LocalAstronaut player, ref HitInfo hit)
        {

            base.Use(player, ref hit);
            ResourceProcessingGUI.Activate(this, player);
        }


    }


}
