using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
{
    public class ResourceProcessingBlock : InteractiveBlock
    {

        protected Recipe Recipe;

        public Storage InputStorage { get; private set; } = new Storage(1, 1);
        public Storage FuelStorage { get; private set; } = new Storage(1, 1);
        public Storage OutputStorage { get; private set; } = new Storage(1, 1);

        public bool IsRunning { get; private set; } = false;

        private readonly string blockType;
        public ResourceProcessingBlock(BlockData blockData) : base(blockData)
        {
            blockType = blockData.Type;
            InputStorage.Name = "InputStorage";
            FuelStorage.Name = "FuelStorage";
            OutputStorage.Name = "OutputStorage";
            InputStorage.GetSlot(0, 0).Name = "Input";
            FuelStorage.GetSlot(0, 0).Name = "Fuel";
            OutputStorage.GetSlot(0, 0).Name = "Output";
        }

        public bool HasInput()
        {
            return InputStorage.GetSlot(0, 0).Count > 0;
        }

        public bool TryStartTask(out ProcessResourceTask task)
        {
            task = null;

            if (!HasInput()) return false;
            if (IsRunning) return false;

            if (GameBlocks.TryGetRecipe(blockType, InputStorage.GetSlot(0, 0).Item.Id, out Recipe))
            {

            }
            else return false;


            if (Recipe == null) return false;

            IsRunning = true;
            task = new ProcessResourceTask(Recipe.RequiredTicks, this);

            return true;
        }

        private static bool ValidateInput(ItemSlot inSlot, Recipe recipe)
        {
            return inSlot.HasItem && inSlot.Item.Id == recipe.Ingredient.Item.Id && inSlot.Count >= recipe.Ingredient.Quantity;
        }

        private static bool ValidateOutput(ItemSlot outSlot, Recipe recipe)
        {
            bool sameId = true;
            if (outSlot.HasItem)
            {
                sameId = outSlot.Item.Id == recipe.Product.Item.Id;
            }
            return outSlot.Count < recipe.Product.Item.StackSize && outSlot.Count + recipe.Product.Quantity <= recipe.Product.Item.StackSize && sameId;
        }

        public void Craft()
        {

            if (Recipe == null)
            {
                IsRunning = false;
                return;
            }

            var inSlot = InputStorage.GetSlot(0, 0);
            var outSlot = OutputStorage.GetSlot(0, 0);

            if (!ValidateInput(inSlot, Recipe))
            {
                IsRunning = false;
                return;
            }
            if (!ValidateOutput(outSlot, Recipe))
            {
                IsRunning = false;
                return;
            }

            inSlot.Count -= Recipe.Ingredient.Quantity;

            if (outSlot.HasItem)
            {
                outSlot.Count += Recipe.Product.Quantity;
            }
            else
            {
                OutputStorage.TryAddItem(Recipe.Product.Item, Recipe.Product.Quantity);
            }

            if (!HasInput()) IsRunning = false;
        }

    }

}
