using System.Numerics;
using ImGuiNET;
using Engine;
using Engine.Audio;

using Spacebox.Game.Player;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player.GameModes;
using Engine.InputPro;

namespace Spacebox.Game.GUI
{
    public static class InventoryUI
    {
        private static float SlotSize = 64.0f;
        private static nint SlotTexture = nint.Zero;
        private static nint ItemTexture = nint.Zero;

        public static LocalAstronaut Player;

        public static AudioSource splitAudio;

        public static void Initialize(nint textureId)
        {
            SlotTexture = textureId;

            ItemTexture = new Texture2D("Resources/Textures/UI/trash.png", true, false).Handle;
            InventoryUIHelper.SetDefaultIcon(textureId, nint.Zero);

            splitAudio = new AudioSource(Resources.Load<AudioClip>("splitStack"));
        }

        private static void HandleInput()
        {
            if (Input.IsActionDown("inventory") && !Debug.IsVisible)
            {
                if (Player.IsAlive == false) return;
                if (Chat.FocusInput) return; 

                if (!UIManager.IsUIMode)
                {
                    if (Player.GameMode != GameMode.Survival)
                    {
                        UIManager.Open("inventory", "creative");
                    }
                    else
                    {
                        UIManager.Open("inventory");
                    }
                }
                else
                {

                    UIManager.CloseAll();
                }

                if (UIManager.IsOpen("inventory"))
                {
                    ItemControlsUI.IsVisible = true;

                    var shift = InputManager.Instance.GetAction("storage_item_quick_transfer");
                    var delete = InputManager.Instance.GetAction("storage_item_delete");

                    var text = shift.Bindings[0].GetDisplayName() + " - " + shift.Description;
                    text += "\n" + delete.Bindings[0].GetDisplayName() + " - " + delete.Description;

                    ItemControlsUI.Text = text;
                }
                else
                {
                    ItemControlsUI.IsVisible = false;
                }
            }
        }

        public static void OnGUI(Storage storage)
        {
            HandleInput();

            if (!UIManager.IsOpen("inventory")) return;

            if (storage == null) return;

            var displaySize = ImGui.GetIO().DisplaySize;
            var style = ImGui.GetStyle();

            float titleBarHeight = ImGui.GetFontSize() + style.FramePadding.Y * 2;

            SlotSize = InventoryUIHelper.SlotSize;

            float windowWidth = storage.SizeX * SlotSize;
            float windowHeight = storage.SizeY * SlotSize;

            Vector2 windowPos = new Vector2(
                (displaySize.X - windowWidth) / 2f,
                (displaySize.Y - windowHeight) / 1.6f
            );

            var padding = SlotSize * 0.1f;
            var paddingV = new Vector2(padding, padding);

            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight + padding * 4) + paddingV + paddingV);
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoDecoration |
                                           ImGuiWindowFlags.NoScrollbar |
                                           ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoScrollWithMouse;

            ImGui.Begin("Inventory", windowFlags);

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight + padding * 4) + paddingV + paddingV, displaySize.Y);

            ImGui.SetCursorPos(paddingV);
            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1f), "Inventory");
            InventoryUIHelper.SortStorageButtons(windowWidth, padding, storage);

            ImGui.SetCursorPos(paddingV + new Vector2(0, padding * 4));
            InventoryUIHelper.RenderStorage(storage, OnSlotClicked, storage.SizeX);

            ImGui.End();
        }

        private static void OnSlotClicked(ItemSlot slot)
        {
            if (slot.HasItem)
            {
                if (Input.IsAction("storage_item_quick_transfer"))
                {
                    slot.TryMoveItemToConnectedStorage(out var _);
                }
                if (Input.IsAction("storage_item_delete"))
                {
                    slot.Clear();
                }
            }
        }
    }
}