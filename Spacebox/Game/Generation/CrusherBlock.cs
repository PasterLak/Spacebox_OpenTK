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
            OnUse += CrusherGUI.Toggle;
        }

        public override void Use(Astronaut player)
        {
            CrusherGUI.Activate(this, player);
            base.Use(player);
        }

    }
}
