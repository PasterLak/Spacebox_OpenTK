using Spacebox.Common;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
{
    public class FurnaceBlock : ResourceProcessingBlock
    {

        public FurnaceBlock(BlockData blockData) : base(blockData)
        {
            OnUse += FurnaceGUI.Toggle;

        }

        public override void Use(Astronaut player)
        {
            FurnaceGUI.Activate(this, player);
            base.Use(player);
        }


    }
}
