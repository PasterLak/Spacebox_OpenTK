using ImGuiNET;
using Spacebox.Common;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using Spacebox.UI;
using System.Numerics;

namespace Spacebox.Game.GUI
{
    public class CrusherGUI
    {
        private static Recipe Recipe = new Recipe();
        private static Storage InputStorage = new Storage(1,1);
        private static Storage FuelStorage = new Storage(1, 1);
        private static Storage OutputStorage = new Storage(1, 1);

       
        private static float TimeToCrush = 2f;
        private static short _time = 0;
        public static bool IsVisible { get; set; } = false;

        public static void Toggle()
        {
            IsVisible = !IsVisible;
          
        }

        public static void Init()
        {
            Recipe.Ingredient = new Ingredient(GameBlocks.GetItemByName("Aluminium Ore"),1);
            Recipe.Product = new Product(GameBlocks.GetItemByName("Aluminium Dust"), 1);

            //Recipe.Ingredient = new Ingredient(GameBlocks.GetItemByName("Ice Shards"), 2);
            //Recipe.Product = new Product(GameBlocks.GetItemByName("Water"), 1);
            Recipe.RequiredTicks = 40;

            TickTask createItemTask = new TickTask(Recipe.RequiredTicks, Craft);

            TickTaskManager.AddTask(createItemTask);
        }
        public void Open(Astronaut player, InteractiveBlock block)
        {

        }

        private static void Craft()
        {
            
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
        }

        public static void Update()
        {
            return;
            if (InputStorage.GetSlot(0, 0).HasItem && InputStorage.GetSlot(0, 0).Item.Id == Recipe.Ingredient.Item.Id)
            {
                if (OutputStorage.GetSlot(0, 0).HasItem)
                {
                    if (OutputStorage.GetSlot(0, 0).Item.Id == Recipe.Product.Item.Id)
                    {
                        if (OutputStorage.GetSlot(0, 0).HasFreeSpace)
                        {
                            //_time += Time.tick;

                            if (_time >= TimeToCrush)
                            {
                                _time = 0;
                                InputStorage.GetSlot(0, 0).Count -= Recipe.Ingredient.Quantity;

                                if (OutputStorage.GetSlot(0, 0).HasItem)
                                {
                                    OutputStorage.GetSlot(0, 0).Count += Recipe.Product.Quantity;
                                }
                            }
                        }
                    }

                }
                else
                {
                    if (InputStorage.GetSlot(0, 0).Item.Id == Recipe.Ingredient.Item.Id)
                    {

                       // _time += Time.Delta;

                        if (_time >= TimeToCrush)
                        {
                            _time = 0;
                            InputStorage.GetSlot(0, 0).DropOne();


                            OutputStorage.TryAddItem(Recipe.Product.Item, 1);

                        }

                    }
                }


            }

        }

        public static void OnGUI()
        {
            if (!IsVisible) return;
            Vector2 displaySize = ImGui.GetIO().DisplaySize;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));
            float windowWidth = displaySize.Y * 0.3f;
            float windowHeight = displaySize.Y * 0.3f;
            var windowPos = GameMenu.CenterNextWindow(windowWidth, windowHeight);

            ImGui.Begin("Crusher", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove 
                | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);

            float buttonWidth = windowWidth * 0.9f;
            float buttonHeight = windowHeight * 0.12f;
            float spacing = windowHeight * 0.03f;

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);

            var space = windowHeight * 0.1f;
            var slotSize = InventoryUIHelper.SlotSize;

            var textSize = ImGui.CalcTextSize("Crusher");

           
            ImGui.SetCursorPos(new Vector2(windowWidth * 0.5f - textSize.X *0.5f, textSize.Y));
            ImGui.Text("Crusher");
            ImGui.SetCursorPos(new Vector2(space, space));
            InventoryUIHelper.DrawSlot(InputStorage.GetSlot(0,0), "InputStorage", null, false);
            //ImGui.SetCursorPos(windowPos + new Vector2(0, InventoryUIHelper.SlotSize) );
            ImGui.SetCursorPos(new Vector2(space , space * 2f + slotSize));
            InventoryUIHelper.DrawSlot(FuelStorage.GetSlot(0, 0), "FuelStorage", null, false);

            ImGui.SetCursorPos(new Vector2(windowWidth - slotSize - space,  slotSize + space));
            InventoryUIHelper.DrawSlot(OutputStorage.GetSlot(0, 0), "OutputStorage", null, false);

            float totalButtonsHeight = buttonHeight * 3 + spacing * 2;
            /*ImGui.Dummy(new Vector2(0, (windowHeight - totalButtonsHeight) / 4));

            GameMenu.CenterButtonWithBackground("Play", buttonWidth, buttonHeight, () => { });
            ImGui.Dummy(new Vector2(0, spacing * 2));
            GameMenu.CenterButtonWithBackground("Options", buttonWidth, buttonHeight, () => { });
            ImGui.Dummy(new Vector2(0, spacing));
            ImGui.Dummy(new Vector2(0, (windowHeight - totalButtonsHeight) / 2));
            GameMenu.CenterButtonWithBackground("Exit", buttonWidth, buttonHeight, () => Window.Instance.Quit());
            */
            
            ImGui.PopStyleColor(6);
            ImGui.End();
        }
    }
}
