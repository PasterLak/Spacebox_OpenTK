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
        }

        public override void Use(Astronaut player, ref HitInfo hit)
        {

            base.Use(player, ref hit);
            ResourceProcessingGUI.Activate(this, player);
        }


    }


}
