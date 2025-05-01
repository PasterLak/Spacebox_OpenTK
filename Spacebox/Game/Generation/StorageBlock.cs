using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation
{
    public class StorageBlock : InteractiveBlock
    {
        private Storage storage;

        public StorageBlock(BlockData blockData) : base(blockData)
        {
            storage = new Storage(3,3);
        }

        public override void Use(Astronaut player)
        {
            base.Use(player);

            if(storage != null)
            {
                StorageUI.OpenStorage(storage, player);
            }
        }
    }
}
