
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
        protected static ResourceProcessingBlock crusherBlock;

        protected static bool _isVisible = false;
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

        public static void Toggle(Astronaut player)
        {
            IsVisible = !IsVisible;
        }

        public static void Activate(ResourceProcessingBlock block, Astronaut player)
        {
            crusherBlock = block;

            if (InputStorage != null)
            {
                InputStorage.OnDataWasChanged -= OnInputItemWasChanged;
            }

            OutputStorage.DisconnectStorage();
            

            InputStorage = crusherBlock.InputStorage;
            FuelStorage = crusherBlock.FuelStorage;
            OutputStorage = crusherBlock.OutputStorage;

            OutputStorage.ConnectStorage(player.Inventory, false);

            InputStorage.OnDataWasChanged += OnInputItemWasChanged;


            if (block.TryStartTask(out var task))
            {
                TickTaskManager.AddTask(task);
            }

        }

        private static void OnInputItemWasChanged(Storage storage)
        {
            if (crusherBlock != null)
            {
                if (crusherBlock.TryStartTask(out var task))
                {
                    TickTaskManager.AddTask(task);
                }
            }
        }

        public static void OnGUI(string windowName)
        {
            if (!_isVisible) return;

            Vector2 displaySize = ImGui.GetIO().DisplaySize;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));
            float windowWidth = displaySize.Y * 0.4f;
            float windowHeight = displaySize.Y * 0.25f;
            var windowPos = GameMenu.CenterNextWindow3(windowWidth, windowHeight);


            ImGui.Begin(windowName, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);

            float buttonWidth = windowWidth * 0.9f;
            float buttonHeight = windowHeight * 0.12f;
            float spacing = windowHeight * 0.03f;

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);

            var space = windowHeight * 0.1f;
            var slotSize = InventoryUIHelper.SlotSize;

            var textSize = ImGui.CalcTextSize(windowName);


            ImGui.SetCursorPos(new Vector2(windowWidth * 0.5f - textSize.X * 0.5f, textSize.Y));
            ImGui.Text(windowName);
            ImGui.SetCursorPos(new Vector2(space, space ));
            InventoryUIHelper.DrawSlot(InputStorage.GetSlot(0, 0), "InputStorage", null, false);
            InventoryUIHelper.ShowTooltip(InputStorage.GetSlot(0, 0), true);
            ImGui.SetCursorPos(new Vector2(space, space * 2f + slotSize));
            InventoryUIHelper.DrawSlot(FuelStorage.GetSlot(0, 0), "FuelStorage", null, false);
            InventoryUIHelper.ShowTooltip(FuelStorage.GetSlot(0, 0), true);
            ImGui.SetCursorPos(new Vector2(windowWidth - slotSize - space, slotSize + space));
            InventoryUIHelper.DrawSlot(OutputStorage.GetSlot(0, 0), "OutputStorage", MoveItems, false);
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
