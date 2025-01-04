using Spacebox.Common;
using Spacebox.Game.GUI;
using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
{
    public class FurnaceBlock : InteractiveBlock
    {
        private Recipe Recipe;

        public Storage InputStorage { get; private set; } = new Storage(1, 1);
        public Storage FuelStorage { get; private set; } = new Storage(1, 1);
        public Storage OutputStorage { get; private set; } = new Storage(1, 1);

        public bool IsRunning { get; private set; } = false;
        public FurnaceBlock(BlockData blockData) : base(blockData)
        {
            OnUse += FurnaceGUI.Toggle;

        }

        public override void Use()
        {
            FurnaceGUI.Activate(this);
            base.Use();
        }

        public bool HasInput()
        {
            return InputStorage.GetSlot(0, 0).Count > 0;
        }

        public bool TryStartTask(out FurnaceTask task)
        {
            task = null;

            if (!HasInput()) return false;
            if (IsRunning) return false;

            if (GameBlocks.TryGetRecipe("furnace", InputStorage.GetSlot(0, 0).Item.Id, out Recipe))
            {

            }
            else return false;


            if (Recipe == null) return false;

            IsRunning = true;
            task = new FurnaceTask(Recipe.RequiredTicks, this);

            return true;
        }

        public void Craft()
        {

            if (Recipe == null)
            {
                IsRunning = false;
                return;
            }

            if (InputStorage.GetSlot(0, 0).HasItem && InputStorage.GetSlot(0, 0).Item.Id == Recipe.Ingredient.Item.Id)
            {
                if (OutputStorage.GetSlot(0, 0).HasItem)
                {
                    if (OutputStorage.GetSlot(0, 0).Item.Id == Recipe.Product.Item.Id)
                    {
                        if (OutputStorage.GetSlot(0, 0).HasFreeSpace)
                        {

                            InputStorage.GetSlot(0, 0).Count -= Recipe.Ingredient.Quantity;

                            if (OutputStorage.GetSlot(0, 0).HasItem)
                            {
                                OutputStorage.GetSlot(0, 0).Count += Recipe.Product.Quantity;
                            }

                        }
                    }

                }
                else
                {
                    if (InputStorage.GetSlot(0, 0).Item.Id == Recipe.Ingredient.Item.Id)
                    {

                        InputStorage.GetSlot(0, 0).DropOne();

                        OutputStorage.TryAddItem(Recipe.Product.Item, 1);

                    }
                }


            }

            if (!HasInput()) IsRunning = false;
        }
    }
}
