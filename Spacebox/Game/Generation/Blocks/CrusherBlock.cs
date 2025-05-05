using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks
{
    public class CrusherBlock : ResourceProcessingBlock
    {

        public CrusherBlock(BlockData blockData) : base(blockData)
        {
            OnUse += ResourceProcessingGUI.Toggle;

            SetEmissionWithoutRedrawChunk(false);
        }

        public override void Use(Astronaut player)
        {

            base.Use(player);
            ResourceProcessingGUI.Activate(this, player);
        }

    }
}
