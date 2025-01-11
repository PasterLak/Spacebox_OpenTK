using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
{
    public class ResourceProcessingBlock : InteractiveBlock
    {

        protected Recipe Recipe;

        public Storage InputStorage { get; private set; } = new Storage(1, 1);
        public Storage FuelStorage { get; private set; } = new Storage(1, 1);
        public Storage OutputStorage { get; private set; } = new Storage(1, 1);
        public float Efficiency = 1f;
        private float TestingCoefficient = 1.5f;
        private bool _isRunning = false;
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;
                SetEmission(_isRunning);
            }
        }

        public string WindowName;
        
        private readonly string blockType;

        public ResourceProcessingBlock(BlockData blockData) : base(blockData)
        {
            Efficiency = blockData.Efficiency;
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

        public void GiveAllResourcesBack(Storage storage)
        {
            var inputItem = InputStorage.GetSlot(0, 0);
            var fuelItem = FuelStorage.GetSlot(0, 0);
            var outputItem = OutputStorage.GetSlot(0, 0);

            if (inputItem.Count > 0)
            {
                storage.TryAddItem(inputItem.Item, inputItem.Count);
            }
            if (outputItem.Count > 0)
            {
                storage.TryAddItem(outputItem.Item, outputItem.Count);
            }
            if (fuelItem.Count > 0)
            {
                storage.TryAddItem(fuelItem.Item, fuelItem.Count);
            }
            
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

            if (!ValidateInput(InputStorage.GetSlot(0, 0), Recipe)) return false;
            if (!ValidateOutput(OutputStorage.GetSlot(0, 0), Recipe)) return false;

            IsRunning = true;

            
            task = new ProcessResourceTask((int)(Recipe.RequiredTicks * TestingCoefficient / Efficiency), this);

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
