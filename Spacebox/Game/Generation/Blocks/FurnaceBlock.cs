using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks
{
    public class FurnaceBlock : ResourceProcessingBlock
    {

        public FurnaceBlock(BlockData blockData) : base(blockData)
        {
            OnUse += ResourceProcessingGUI.Toggle;

            //LightLevel

            SetEmission(false,false);
        }

        public override void Use(LocalAstronaut player, ref HitInfo hit)
        {
            base.Use(player, ref hit);
            ResourceProcessingGUI.Activate(this, player);
        }


    }
}
