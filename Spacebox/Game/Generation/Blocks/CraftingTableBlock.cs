using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks
{
    public class CraftingTableBlock : InteractiveBlock
    {
        public CraftingTableBlock(BlockData blockData) : base(blockData)
        {
            OnUse += CraftingGUI.Toggle;

        }

        public override void Use(Astronaut player)
        {
            base.Use(player);
        }

    }


}

