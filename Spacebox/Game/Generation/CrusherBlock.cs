using Spacebox.Common;
using Spacebox.Game.GUI;
using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
{
    public class CrusherBlock : InteractiveBlock
    {
        private Recipe Recipe;

        private static Dictionary<short, Recipe> recipes;

        public Storage InputStorage { get; private set; } = new Storage(1, 1);
        public Storage FuelStorage { get; private set; } = new Storage(1, 1);
        public Storage OutputStorage { get; private set; } = new Storage(1, 1);

        public bool IsRunning { get; private set; } = false;
        public CrusherBlock(BlockData blockData) : base(blockData)
        {
            OnUse += CrusherGUI.Toggle;
           

            if(recipes == null)
            {
                recipes = new Dictionary<short, Recipe>();

                Recipe r = new Recipe();
                r.Ingredient = new Ingredient(GameBlocks.GetItemByName("Aluminium Ore"), 1);
                r.Product = new Product(GameBlocks.GetItemByName("Aluminium Dust"), 1);
                r.RequiredTicks = 40;
                recipes.Add(r.Ingredient.Item.Id, r);

                Recipe r2 = new Recipe();
                r2.Ingredient = new Ingredient(GameBlocks.GetItemByName("Iron Ore"), 1);
                r2.Product = new Product(GameBlocks.GetItemByName("Iron Dust"), 1);
                r2.RequiredTicks = 40;
                recipes.Add(r2.Ingredient.Item.Id, r2);

                Recipe r3 = new Recipe();
                r3.Ingredient = new Ingredient(GameBlocks.GetItemByName("Ice"), 1);
                r3.Product = new Product(GameBlocks.GetItemByName("Ice Shards"), 1);
                r3.RequiredTicks = 40;
                recipes.Add(r3.Ingredient.Item.Id, r3);
            }
        }

        public override void Use()
        {
           CrusherGUI.Activate(this);
            base.Use();
        }

        public bool HasInput()
        {
            return InputStorage.GetSlot(0, 0).Count > 0;
        }

        public bool TryStartTask(out CrusherTask task)
        {
            task = null;

            if (!HasInput()) return false;
            if(IsRunning) return false;

            if (!recipes.ContainsKey(InputStorage.GetSlot(0, 0).Item.Id)) return false;

            Recipe = recipes[InputStorage.GetSlot(0, 0).Item.Id];

            if(Recipe == null) return false;

            IsRunning = true;
            task = new CrusherTask(Recipe.RequiredTicks, this);

            return true;
        }

        public void Craft()
        {

            if(Recipe == null)
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

            if(!HasInput()) IsRunning = false;
        }
    }
}
