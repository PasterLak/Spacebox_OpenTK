using System.Numerics;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI
{
    public static class InventoryUI
    {
        private static float SlotSize = 64.0f;
        private static nint SlotTexture = nint.Zero;
        private static nint ItemTexture = nint.Zero;

        public static bool IsVisible { get; set; } = false;

        public static Astronaut Player;

        public static void Initialize(nint textureId)
        {
            SlotTexture = textureId;

            ItemTexture = new Texture2D("Resources/Textures/item.png", true, false).Handle;
            InventoryUIHelper.SetDefaultIcon(textureId, nint.Zero);

            var inventory = ToggleManager.Register("inventory");
            inventory.IsUI = true;
            inventory.OnStateChanged += s =>
            {
                IsVisible = s;
            };
        }
        private static void HandleInput()
        {
            if (Input.IsKeyDown(Keys.Tab) && !Debug.IsVisible)
            {
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


                }
                else
                {
                    ToggleManager.SetState("mouse", false);
                    ToggleManager.SetState("player", true);
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

            //ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.5f, 0.5f, 0.5f, 1f));
            //ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.5f, 0.5f, 0.5f, 1f));
            //ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.5f, 0.5f, 0.5f, 1f));

            ImGui.Begin("Inventory", windowFlags);

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight + padding * 4) + paddingV + paddingV, displaySize.Y);

            ImGui.SetCursorPos(paddingV );
            ImGui.TextColored( new Vector4(0.9f, 0.9f, 0.9f, 1f), "Inventory");
            //ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            //ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, Vector2.Zero);
            //ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(15, 15));

            //ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(2,2));

            ImGui.SetCursorPos(paddingV + new Vector2(0, padding * 4));
            InventoryUIHelper.RenderStorage(storage, OnSlotClicked, storage.SizeX);

            //ImGui.PopStyleColor(3);
            //ImGui.PopStyleVar(3);
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
                if (Input.IsKey(Keys.LeftAlt))
                {

                    slot.Split();

                }
            }
            else
            {
            }
        }
    }
}
