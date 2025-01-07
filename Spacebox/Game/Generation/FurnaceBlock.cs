
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
{
    public class FurnaceBlock : ResourceProcessingBlock
    {

        public FurnaceBlock(BlockData blockData) : base(blockData)
        {
            OnUse += ResourceProcessingGUI.Toggle;
            WindowName = "Furnace";
            SetEmissionWithoutRedrawChunk(false);
        }

        public override void Use(Astronaut player)
        {
            ResourceProcessingGUI.Activate(this, player);
            base.Use(player);
        }


    }
}
