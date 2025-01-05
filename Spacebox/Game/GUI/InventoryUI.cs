using System.Numerics;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Game;
using Spacebox.Game.Player;
using Spacebox.UI;

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

            var inventory = ToggleManager.Instance.Register("inventory");

            inventory.OnStateChanged += s =>
            {
                IsVisible = s;
            };
        }
        private static void HandleInput()
        {
            if (Input.IsKeyDown(Keys.C) && !Debug.IsVisible)
            {
                IsVisible = !IsVisible;

                if (IsVisible)
                {
                    Input.ShowCursor();
                    if (Player != null)
                        Player.CanMove = false;
                }
                else
                {
                    Input.HideCursor();
                    if (Player != null)
                        Player.CanMove = true;
                }
            }
        }
        public static void OnGUI(Storage storage)
        {
            HandleInput();

            if (!IsVisible) return;

            if (storage == null) return;


            ImGuiIOPtr io = ImGui.GetIO();

            var style = ImGui.GetStyle();


            float titleBarHeight = ImGui.GetFontSize() + style.FramePadding.Y * 2;



            SlotSize = Math.Clamp(io.DisplaySize.X * 0.04f, 32.0f, 128.0f);
            float windowWidth = storage.SizeX * SlotSize + titleBarHeight;
            float windowHeight = storage.SizeY * SlotSize + titleBarHeight + titleBarHeight;
            Vector2 displaySize = io.DisplaySize;
            Vector2 windowPos = new Vector2(
                (displaySize.X - windowWidth) / 2,
                (displaySize.Y - windowHeight) / 1.6f
            );
            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoCollapse |
                                           //ImGuiWindowFlags.NoMove |

                                           ImGuiWindowFlags.NoScrollbar |
                                           ImGuiWindowFlags.NoScrollWithMouse;

            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.5f, 0.5f, 0.5f, 1f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.5f, 0.5f, 0.5f, 1f));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.5f, 0.5f, 0.5f, 1f));

            ImGui.Begin("Inventory", windowFlags);

            //ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            //ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(15, 15));

            //ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(2,2));


            InventoryUIHelper.RenderStorage(storage, OnSlotClicked, storage.SizeX);

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar(3);
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
