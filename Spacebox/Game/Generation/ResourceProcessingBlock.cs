
using Spacebox.Game.Resources;
using Engine;
namespace Spacebox.Game.Generation
{
    public class ResourceProcessingBlock : InteractiveBlock
    {

        protected Recipe Recipe;

        public Storage InputStorage { get; private set; } = new Storage(1, 1);

        public Storage OutputStorage { get; private set; } = new Storage(1, 1);
        public Storage FuelStorage { get; private set; } = new Storage(1, 1);
        public float Efficiency = 1f;
        private float TestingCoefficient = 1f;

        public Action<ResourceProcessingBlock> OnCrafted;
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

        public TickTask Task { get; private set; }


        private readonly string blockType;

        public Storage[] GetAllStorages()
        {
            return new Storage[3] { InputStorage, OutputStorage, FuelStorage };
        }
        public ItemSlot[] GetAllSlots()
        {
            return new ItemSlot[3] { InputStorage.GetSlot(0, 0), OutputStorage.GetSlot(0, 0), FuelStorage.GetSlot(0, 0) };
        }

        public void SetStorageAfterLoadFromNBT(Item item, byte count, Storage storage)
        {
            storage.SetSlot(0, 0, item, count);
        }

        private short craftTicks = 0;
        private short currentTick = 0;
        public int GetTaskProgress()
        {
            if (craftTicks == 0 || currentTick == 0) return 0;

            return (int)(currentTick / (float)craftTicks * 100f);
        }

        public void TryStart()
        {
            if (InputStorage != null)
            {
                if (InputStorage.GetSlot(0, 0).HasItem)
                {
                    if (TryStartTask(out var t))
                    {
                        TickTaskManager.AddTask(t);
                    }
                }
            }
        }

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

            InputStorage.OnDataWasChanged += OnAnySlotWasChanged;
            FuelStorage.OnDataWasChanged += OnAnySlotWasChanged;
            OutputStorage.OnDataWasChanged += OnAnySlotWasChanged;
        }

        private void OnAnySlotWasChanged(Storage s)
        {
            if (chunk != null)
            {
                chunk.IsModified = true;
            }
        }

        public void StopTask()
        {
            if (Task != null)
            {
                if (Task.IsRunning)
                    Task.Stop();

                Task = null;
                IsRunning = false;
            }
        }


        public bool HasInput()
        {
            return InputStorage.GetSlot(0, 0).Count > 0;
        }
        public bool HasOutput()
        {
            return OutputStorage.GetSlot(0, 0).Count > 0;
        }
        public bool HasFuel()
        {
            return FuelStorage.GetSlot(0, 0).Count > 0;
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

            if (!ValidateInput(InputStorage.GetSlot(0, 0), Recipe))
            {
                Reset();
                return false;
            }
            if (!ValidateOutput(OutputStorage.GetSlot(0, 0), Recipe))
            {
                Reset();
                return false;
            }

            IsRunning = true;

            var ticksRequared = (int)(Recipe.RequiredTicks * TestingCoefficient / Efficiency);
            craftTicks = (short)ticksRequared;
            currentTick = 0;

            task = new ProcessResourceTask(ticksRequared, this);
            Task = task;
            task.OnTick += OnTick;
            return true;
        }

        private void OnTick()
        {
            if (!IsRunning) return;

            currentTick++;

            if (currentTick >= craftTicks)
            {

                currentTick = 0;
            }
        }

        private static bool ValidateInput(ItemSlot inSlot, Recipe recipe)
        {
            return inSlot.HasItem && inSlot.Item.Id == recipe.Ingredient.Item.Id && inSlot.Count >= recipe.Ingredient.Quantity;
        }

        private static bool ValidateOutput(ItemSlot outSlot, Recipe recipe)
        {
            if (!outSlot.HasItem) return true;


            if (outSlot.Item.Id != recipe.Product.Item.Id)
                return false;

            if (outSlot.Count + recipe.Product.Quantity > recipe.Product.Item.StackSize)
                return false;

            return true;
        }

        private void Reset()
        {
            Recipe = null;
            currentTick = 0;
            craftTicks = 0;
            IsRunning = false;
        }
        public void Craft()
        {

            if (Recipe == null)
            {
                craftTicks = 0;
                currentTick = 0;
                IsRunning = false;
                return;
            }

            var inSlot = InputStorage.GetSlot(0, 0);
            var outSlot = OutputStorage.GetSlot(0, 0);

            if (!ValidateInput(inSlot, Recipe))
            {
                IsRunning = false;
                craftTicks = 0;
                currentTick = 0;
                return;
            }
            if (!ValidateOutput(outSlot, Recipe))
            {
                IsRunning = false;
                craftTicks = 0;
                currentTick = 0;
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
            OnCrafted?.Invoke(this);
            if (!HasInput())
            {
                craftTicks = 0;
                currentTick = 0;
                IsRunning = false;

            }


        }

    }

}
