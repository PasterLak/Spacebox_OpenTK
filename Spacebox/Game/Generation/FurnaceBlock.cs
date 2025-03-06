
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation
{
    public class FurnaceBlock : ResourceProcessingBlock
    {

        public FurnaceBlock(BlockData blockData) : base(blockData)
        {
            OnUse += ResourceProcessingGUI.Toggle;
           
            //LightLevel
            SetEmissionWithoutRedrawChunk(false);
        }

        public override void Use(Astronaut player)
        {
            base.Use(player);
            ResourceProcessingGUI.Activate(this, player);
        }


    }
}
