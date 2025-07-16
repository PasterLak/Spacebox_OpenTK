using System.Numerics;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Engine;
using Engine.Audio;

using Spacebox.Game.Player;
using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI
{
    public static class InventoryUI
    {
        private static float SlotSize = 64.0f;
        private static nint SlotTexture = nint.Zero;
        private static nint ItemTexture = nint.Zero;

        public static bool IsVisible { get; set; } = false;

        public static Astronaut Player;


        public static AudioSource splitAudio;
        public static void Initialize(nint textureId)
        {
            SlotTexture = textureId;

            ItemTexture = new Texture2D("Resources/Textures/UI/trash.png", true, false).Handle;
            InventoryUIHelper.SetDefaultIcon(textureId, nint.Zero);

            var inventory = ToggleManager.Register("inventory");
            inventory.IsUI = true;
            inventory.OnStateChanged += s =>
            {
                IsVisible = s;
            };

            splitAudio = new AudioSource(Resources.Load<AudioClip>("splitStack"));
        }
        private static void HandleInput()
        {
            if (Input.IsKeyDown(Keys.Tab) && !Debug.IsVisible)
            {
                if (ToggleManager.IsActiveAndExists("pause")) return;
                var v = IsVisible;

                bool count = ToggleManager.IsActiveAndExists("radar");

                ToggleManager.DisableAllWindows();


                if (!count) 
                    IsVisible = !v;

                if (IsVisible)
                {
                    ToggleManager.SetState("mouse", true);
                    ToggleManager.SetState("player", false);
                    if (Player.GameMode != GameMode.Survival)
                        ToggleManager.SetState("creative", true);

                    ToggleManager.SetState("inventory", true);
                }
                else
                {
                    ToggleManager.SetState("mouse", false);
                    ToggleManager.SetState("player", true);
                    ToggleManager.SetState("inventory", false);
                }

                ToggleManager.SetState("panel", !IsVisible);

            }
        }
        public static void OnGUI(Storage storage)
        {
            HandleInput();

            if (!IsVisible) return;

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

            ImGui.SetNextWindowPos(windowPos , ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight + padding * 4) + paddingV + paddingV);
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoDecoration |
                                           ImGuiWindowFlags.NoScrollbar |
                                          ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoScrollWithMouse;


            ImGui.Begin("Inventory", windowFlags);
          
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight + padding * 4) + paddingV + paddingV, displaySize.Y);

            ImGui.SetCursorPos(paddingV );
            ImGui.TextColored( new Vector4(0.9f, 0.9f, 0.9f, 1f), "Inventory");
            InventoryUIHelper.SortStorageButtons(windowWidth, padding, storage);

            ImGui.SetCursorPos(paddingV + new Vector2(0, padding * 4));
            InventoryUIHelper.RenderStorage(storage, OnSlotClicked, storage.SizeX);

            ImGui.End();
        }

        
        private static void OnSlotClicked(ItemSlot slot)
        {

            if (slot.HasItem)
            {
                if (Input.IsKey(Keys.LeftShift))
                {

                    slot.MoveItemToConnectedStorage();

                }
                if (Input.IsKey(Keys.X))
                {

                    slot.Clear();

                }
        
            }
            else
            {
            }
        }
    }
}
