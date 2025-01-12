using Spacebox.FPS;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
{
    public class CrusherBlock : ResourceProcessingBlock
    {
       
        public CrusherBlock(BlockData blockData) : base(blockData)
        {
            OnUse += ResourceProcessingGUI.Toggle;
            WindowName = "Crusher";
            SetEmissionWithoutRedrawChunk(false);
        }

        public override void Use(Astronaut player)
        {
            
            base.Use(player);
            ResourceProcessingGUI.Activate(this, player);
        }

    }
}
