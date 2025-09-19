using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks
{
    public class CraftingTableBlock : InteractiveBlock
    {
        public CraftingTableBlock(BlockJSON blockData) : base(blockData)
        {
            OnUse += CraftingGUI.Toggle;

        }

        public override void Use(Astronaut player, ref HitInfo hit)
        {
            base.Use(player, ref hit);
        }

    }


}

