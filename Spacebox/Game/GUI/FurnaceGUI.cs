using ImGuiNET;
using Spacebox.Common;
using Spacebox.Game.Generation;
using Spacebox.UI;
using System.Numerics;

namespace Spacebox.Game.GUI
{
    public class FurnaceTask : TickTask
    {
        private FurnaceBlock _crusher;
        public FurnaceTask(int requiredTicks, FurnaceBlock crusher) : base(requiredTicks)
        {
            _crusher = crusher;
            base.OnComplete += OnComplete;
        }

        public void OnComplete()
        {
            if (_crusher == null)
            {
                base.Stop();
                return;
            }
            if (_crusher.Durability == 0)
            {
                base.Stop();
                return;
            }
            if (!_crusher.IsRunning)
            {
                base.Stop();
                return;
            }

            _crusher.Craft();
        }
    }
    public class FurnaceGUI
    {
        private static Recipe Recipe = new Recipe();
        private static Storage InputStorage = new Storage(1, 1);
        private static Storage FuelStorage = new Storage(1, 1);
        private static Storage OutputStorage = new Storage(1, 1);
        private static FurnaceBlock crusherBlock;
        private static float TimeToCrush = 2f;
        private static short _time = 0;

        private static bool _isVisible = false;
        public static bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                ToggleManager.Instance.SetState("player", !_isVisible);
                ToggleManager.Instance.SetState("mouse", _isVisible);
            }
        }

        public static void Activate(FurnaceBlock block)
        {
            crusherBlock = block;

            InputStorage = crusherBlock.InputStorage;
            FuelStorage = crusherBlock.FuelStorage;
            OutputStorage = crusherBlock.OutputStorage;

            if (block.TryStartTask(out var task))
            {
                TickTaskManager.AddTask(task);
            }

        }

        public static void Toggle()
        {
            IsVisible = !IsVisible;
        }

        public static void OnGUI()
        {
            if (!_isVisible) return;
            Vector2 displaySize = ImGui.GetIO().DisplaySize;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));
            float windowWidth = displaySize.Y * 0.3f;
            float windowHeight = displaySize.Y * 0.3f;
            var windowPos = GameMenu.CenterNextWindow(windowWidth, windowHeight);

            ImGui.Begin("Furnace", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);

            float buttonWidth = windowWidth * 0.9f;
            float buttonHeight = windowHeight * 0.12f;
            float spacing = windowHeight * 0.03f;

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);

            var space = windowHeight * 0.1f;
            var slotSize = InventoryUIHelper.SlotSize;

            var textSize = ImGui.CalcTextSize("Furnace");


            ImGui.SetCursorPos(new Vector2(windowWidth * 0.5f - textSize.X * 0.5f, textSize.Y));
            ImGui.Text("Furnace");
            ImGui.SetCursorPos(new Vector2(space, space));
            InventoryUIHelper.DrawSlot(InputStorage.GetSlot(0, 0), "InputStorage", null, false);
            ImGui.SetCursorPos(new Vector2(space, space * 2f + slotSize));
            InventoryUIHelper.DrawSlot(FuelStorage.GetSlot(0, 0), "FuelStorage", null, false);

            ImGui.SetCursorPos(new Vector2(windowWidth - slotSize - space, slotSize + space));
            InventoryUIHelper.DrawSlot(OutputStorage.GetSlot(0, 0), "OutputStorage", null, false);

            ImGui.PopStyleColor(6);
            ImGui.End();
        }
    }
}
