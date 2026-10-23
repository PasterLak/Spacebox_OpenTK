using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks
{
    public class CraftingTableBlock : InteractiveBlock
    {
        public CraftingTableBlock(BlockData blockData) : base(blockData)
        {
            OnUse += ToggleCrafting;

        }

        private static void ToggleCrafting(LocalAstronaut player)
        {
            GameUi.Current?.Crafting.Toggle(player);
        }

        public override void Use(LocalAstronaut player, ref HitInfo hit)
        {
            base.Use(player, ref hit);
        }

    }


}

