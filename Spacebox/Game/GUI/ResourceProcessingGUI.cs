using ImGuiNET;
using Spacebox.Common;
using Spacebox.Common.Audio;
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

        private static string status = "";
        public static bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;

            }
        }

        private static bool wasInitialized = false; //  !!!

        private static AudioSource craftedSound;
        public static void Toggle(Astronaut player)
        {
            if (!wasInitialized)
            {
                var inventory = ToggleManager.Register("resourceProcessing");
                inventory.IsUI = true;
                inventory.OnStateChanged += s =>
                {
                    IsVisible = s;

                    if(!s)
                    {
                        player.Panel.ConnectStorage(player.Inventory, true);
                        player.Inventory.ConnectStorage(player.Panel);
               
                    }
                };

                wasInitialized = true;
            }
           
            var v = !IsVisible;

            ToggleManager.SetState("player", !v);
            ToggleManager.SetState("mouse", v);
            
            if (!v)
            {

                ToggleManager.DisableAllWindows();
               
            }
            else
            {
                ToggleManager.DisableAllWindows();
                ToggleManager.SetState("inventory", v);
                ToggleManager.SetState("creative", v);
                
            }

            ToggleManager.SetState("resourceProcessing", v);

            if (processingBlock != null)
            {
                processingBlock.OnCrafted -= OnCrafted;
            }

            if (craftedSound == null)
            {
                craftedSound = new AudioSource(SoundManager.GetClip("crafted"));
                craftedSound.Volume = 1f;

            }
        }

        private static void InitIcons()
        {
            Debug.Log("----Icons debug " + WindowName);
            if (batteryIcon == IntPtr.Zero)
            {
                var texture = TextureManager.GetTexture("Resources/Textures/UI/battery.png", true);
                texture.FlipY();
                texture.UpdateTexture(true);
                batteryIcon = texture.Handle;
            }

            if (inputIcon == IntPtr.Zero)
            {
                var t1 = TextureManager.GetTexture("Resources/Textures/UI/crusherInput.png", true);
                var t2 = TextureManager.GetTexture("Resources/Textures/UI/furnaceInput.png", true);
                var t3 = TextureManager.GetTexture("Resources/Textures/UI/disassemblerInput.png", true);

                t1.FlipY();
                t1.UpdateTexture(true);
                t2.FlipY();
                t2.UpdateTexture(true);
                t3.FlipY();
                t3.UpdateTexture(true);
            }

            if (WindowName == "Crusher")
            {
                var texture = TextureManager.GetTexture("Resources/Textures/UI/crusherInput.png", true);
                inputIcon = texture.Handle;
            }
            if (WindowName == "Furnace")
            {
                var texture = TextureManager.GetTexture("Resources/Textures/UI/furnaceInput.png", true);
                inputIcon = texture.Handle;
            }
            if (WindowName == "Disassembler")
            {
                var texture = TextureManager.GetTexture("Resources/Textures/UI/disassemblerInput.png", true);
                inputIcon = texture.Handle;
            }


            if (outputIcon == IntPtr.Zero)
            {
                var t1 = TextureManager.GetTexture("Resources/Textures/UI/crusherOutput.png", true);
                var t2 = TextureManager.GetTexture("Resources/Textures/UI/furnaceOutput.png", true);

                t1.FlipY();
                t1.UpdateTexture(true);
                t2.FlipY();
                t2.UpdateTexture(true);
            }

            if (WindowName == "Crusher")
            {
                var texture = TextureManager.GetTexture("Resources/Textures/UI/crusherOutput.png", true);
                outputIcon = texture.Handle;
            }
            if (WindowName == "Furnace")
            {
                var texture = TextureManager.GetTexture("Resources/Textures/UI/furnaceOutput.png", true);
                outputIcon = texture.Handle;
            }
            if (WindowName == "Disassembler")
            {
                var texture = TextureManager.GetTexture("Resources/Textures/UI/furnaceOutput.png", true);
                outputIcon = texture.Handle;
            }

        }

        public static void Activate(ResourceProcessingBlock block, Astronaut player)
        {
            if (!IsVisible) return;
            WindowName = block.WindowName;
            InitIcons();

            processingBlock = block;

            if (InputStorage != null)
            {
                InputStorage.OnDataWasChanged -= OnInputItemWasChanged;
            }
            if (OutputStorage != null)
            {
                OutputStorage.OnDataWasChanged -= OnInputItemWasChanged;
            }
            if (FuelStorage != null)
            {
                FuelStorage.OnDataWasChanged -= OnInputItemWasChanged;
            }
            status = "Status: Ready";
            InputStorage.DisconnectStorage();
            FuelStorage.DisconnectStorage();
            OutputStorage.DisconnectStorage();


            InputStorage = processingBlock.InputStorage;
            FuelStorage = processingBlock.FuelStorage;
            OutputStorage = processingBlock.OutputStorage;

            block.OnCrafted += OnCrafted;

            InputStorage.ConnectStorage(player.Inventory, true);
            FuelStorage.ConnectStorage(player.Inventory, false);
            OutputStorage.ConnectStorage(player.Inventory, false);

            player.Inventory.ConnectStorage(InputStorage, false);
            player.Panel.ConnectStorage(InputStorage, false);

            InputStorage.OnDataWasChanged += OnInputItemWasChanged;
            OutputStorage.OnDataWasChanged += OnInputItemWasChanged;
            FuelStorage.OnDataWasChanged += OnInputItemWasChanged;


            if (block.TryStartTask(out var task))
            {
                TickTaskManager.AddTask(task);

            }
            else
            {

            }
            UpdateStatus();
        }

        private static void OnCrafted(ResourceProcessingBlock block)
        {
            if (!IsVisible) return;

            if (craftedSound.IsPlaying)
            {
                craftedSound.Stop();
                craftedSound.Play();
            }
            else
            {
                craftedSound.Play();
            }

        }

        private static void OnInputItemWasChanged(Storage storage)
        {
            if (processingBlock != null)
            {
                if (processingBlock.TryStartTask(out var task))
                {
                    // status = "Status: Working...";
                    TickTaskManager.AddTask(task);
                }
                else
                {
                    status = "Status: Ready";
                }

                UpdateStatus();

            }

        }

        private static void UpdateStatus()
        {
            if (processingBlock != null)
            {

                if (InputStorage.GetSlot(0, 0).Count == 0)
                {
                    status = "Status: Done";
                    processingBlock.StopTask();
                }

                if (processingBlock.IsRunning)
                {
                    status = "Status: " + processingBlock.GetTaskProgress() + "/100";
                }
                else
                {
                    if (InputStorage.GetSlot(0, 0).Count > 0)
                    {
                        status = "Status: Wrong ingredient!";
                        processingBlock.StopTask();
                    }

                    status = "Status: Done";
                }



            }
            else { status = "Status: Done"; }


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

            if (processingBlock != null)
            {
                if (processingBlock.IsRunning)
                {
                    status = "Status: " + processingBlock.GetTaskProgress() + "/100";
                }
            }

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);

            var space = windowHeight * 0.1f;
            var slotSize = InventoryUIHelper.SlotSize;

            var textSize = ImGui.CalcTextSize(WindowName);

            ImGui.SetCursorPos(new Vector2(windowWidth * 0.505f - textSize.X * 0.5f, textSize.Y));
            ImGui.TextColored(new Vector4(0.1f, 0.1f, 0.1f, 0.1f), WindowName);
            ImGui.SetCursorPos(new Vector2(windowWidth * 0.5f - textSize.X * 0.5f, textSize.Y));
            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 0.9f), WindowName);

            var textSize2 = ImGui.CalcTextSize(status);
            ImGui.SetCursorPos(new Vector2(windowWidth * 0.5f - textSize2.X * 0.5f, textSize.Y * 2.2f));
            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 0.9f), status);

            ImGui.SetCursorPos(new Vector2(space, space * 2));
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

            if (Input.IsKey(OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftShift))
            {
                if (slot.Storage.ConnectedStorage != null)
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
