using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
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

