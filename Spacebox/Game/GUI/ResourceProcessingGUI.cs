
using ImGuiNET;
using Spacebox.Common;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using Spacebox.UI;
using System.Numerics;

namespace Spacebox.Game.GUI
{
    public class ResourceProcessingGUI
    {
        protected static Recipe Recipe = new Recipe();
        protected static Storage InputStorage = new Storage(1, 1);
        protected static Storage FuelStorage = new Storage(1, 1);
        protected static Storage OutputStorage = new Storage(1, 1);
        protected static ResourceProcessingBlock processingBlock;
        public static string WindowName = "";
        private static IntPtr batteryIcon = IntPtr.Zero;
        private static IntPtr inputIcon = IntPtr.Zero;
        private static IntPtr outputIcon = IntPtr.Zero;
        protected static bool _isVisible = false;

        private static StatsGUI statsGUI;
        private static StatsBarData barData;
        public static bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                ToggleManager.Instance.SetState("player", !_isVisible);
                ToggleManager.Instance.SetState("mouse", _isVisible);
                ToggleManager.Instance.SetState("inventory", _isVisible);
            }
        }

        private static bool wasInitialized = true; //  !!!
        public static void Init()
        {
            return;
            barData = new StatsBarData();
            statsGUI = new StatsGUI(barData);


            barData = new StatsBarData
            {
                Count = 50,
                MaxCount = 100,
                Name = "Process"
            };

            statsGUI = new StatsGUI(barData)
            {
                FillColor = new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 1.0f),
                TextColor = new Vector4(1f, 1f, 1f, 1f),
                Size = new Vector2(300, 50),
                Position = new Vector2(1920 /2f, 300),
                Anchor = Anchor.Left,
                WindowName = "Process"
            };
            statsGUI.ShowText = true;

            statsGUI.OnResized(Window.Instance.Size);

            wasInitialized = true;

        }

        public static void Toggle(Astronaut player)
        {
            IsVisible = !IsVisible;
        }

        private static void InitIcons()
        {
            if(batteryIcon == IntPtr.Zero)
            {
                var texture  = TextureManager.GetTexture("Resources/Textures/UI/battery.png", true);
                texture.FlipY();
                texture.UpdateTexture(true);
                batteryIcon = texture.Handle;
            }
            
            if (outputIcon == IntPtr.Zero)
            {
                if(WindowName == "Crusher")
                {
                    var texture = TextureManager.GetTexture("Resources/Textures/UI/crusherOutput.png", true);
                    texture.FlipY();
                    texture.UpdateTexture(true);
                    outputIcon = texture.Handle;
                }
                   
                //if (WindowName == "Furnace")
                 //   outputIcon = TextureManager.GetTexture("Resources/Textures/UI/furnaceOutput.png", true,true).Handle;
            }

            if (inputIcon == IntPtr.Zero)
            {
                if (WindowName == "Crusher")
                {
                    var texture = TextureManager.GetTexture("Resources/Textures/UI/crusherInput.png", true);
                    texture.FlipY();
                    texture.UpdateTexture(true);
                    inputIcon = texture.Handle;
                }

                //if (WindowName == "Furnace")
                //   outputIcon = TextureManager.GetTexture("Resources/Textures/UI/furnaceOutput.png", true,true).Handle;
            }

        }

        public static void Activate(ResourceProcessingBlock block, Astronaut player)
        {
            InitIcons();
            if (!wasInitialized) Init();
            processingBlock = block;
            WindowName = block.WindowName;
            if (InputStorage != null)
            {
                InputStorage.OnDataWasChanged -= OnInputItemWasChanged;
            }

            InputStorage.DisconnectStorage();
            FuelStorage.DisconnectStorage();
            OutputStorage.DisconnectStorage();
            

            InputStorage = processingBlock.InputStorage;
            FuelStorage = processingBlock.FuelStorage;
            OutputStorage = processingBlock.OutputStorage;

            InputStorage.ConnectStorage(player.Inventory, false);
            FuelStorage.ConnectStorage(player.Inventory, false);
            OutputStorage.ConnectStorage(player.Inventory, false);

            InputStorage.OnDataWasChanged += OnInputItemWasChanged;


            if (block.TryStartTask(out var task))
            {
                TickTaskManager.AddTask(task);
            }

        }

        private static void OnInputItemWasChanged(Storage storage)
        {
            if (processingBlock != null)
            {
                if (processingBlock.TryStartTask(out var task))
                {
                    TickTaskManager.AddTask(task);
                }
            }
        }

        public static void OnGUI()
        {
            if (!_isVisible) return;

            Vector2 displaySize = ImGui.GetIO().DisplaySize;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));
            float windowWidth = displaySize.Y * 0.4f;
            float windowHeight = displaySize.Y * 0.25f;
            var windowPos = GameMenu.CenterNextWindow3(windowWidth, windowHeight);


            ImGui.Begin(WindowName, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);

            float buttonWidth = windowWidth * 0.9f;
            float buttonHeight = windowHeight * 0.12f;
            float spacing = windowHeight * 0.03f;

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);

            var space = windowHeight * 0.1f;
            var slotSize = InventoryUIHelper.SlotSize;

            var textSize = ImGui.CalcTextSize(WindowName);

            ImGui.SetCursorPos(new Vector2(windowWidth * 0.505f - textSize.X * 0.5f, textSize.Y));
            ImGui.TextColored(new Vector4(0.1f, 0.1f, 0.1f, 0.1f), WindowName);
            ImGui.SetCursorPos(new Vector2(windowWidth * 0.5f - textSize.X * 0.5f, textSize.Y));
            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 0.9f),WindowName);
           
            ImGui.SetCursorPos(new Vector2(space, space * 2 ));
            InventoryUIHelper.DrawSlot(InputStorage.GetSlot(0, 0), "InputStorage", MoveItems, inputIcon, false);
            InventoryUIHelper.ShowTooltip(InputStorage.GetSlot(0, 0), true);
            ImGui.SetCursorPos(new Vector2(space, windowHeight - slotSize - space));
            InventoryUIHelper.DrawSlot(FuelStorage.GetSlot(0, 0), "FuelStorage", MoveItems, batteryIcon, false);
            InventoryUIHelper.ShowTooltip(FuelStorage.GetSlot(0, 0), true);
            ImGui.SetCursorPos(new Vector2(windowWidth - slotSize - space, slotSize + space));
            InventoryUIHelper.DrawSlot(OutputStorage.GetSlot(0, 0), "OutputStorage", MoveItems, outputIcon, false);
            InventoryUIHelper.ShowTooltip(OutputStorage.GetSlot(0, 0), true);

            
            ImGui.PopStyleColor(6);
            ImGui.End();
        }

        private static void MoveItems(ItemSlot slot)
        {
            if (!slot.HasItem) return;

            if(Input.IsKey(OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftShift))
            {
                if( slot.Storage.ConnectedStorage != null)
                {
                    slot.MoveItemToConnectedStorage();
                }
                else
                {
                    Debug.Error("No connected storage");
                }
            }
        }
    }
}
