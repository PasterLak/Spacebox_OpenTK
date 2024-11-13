using System.Numerics;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Game;
using Spacebox.GUI;

namespace Spacebox.UI
{
    public static class InventoryUI
    {
        private static float SlotSize = 64.0f;
        private static IntPtr SlotTexture = IntPtr.Zero;
        private static IntPtr ItemTexture = IntPtr.Zero;

        public static bool IsVisible { get; set; } = false;

        public static Astronaut Player;

        public static void Initialize(IntPtr textureId)
        {
            SlotTexture = textureId;

            ItemTexture = new Texture2D("Resources/Textures/item.png", true, false).Handle;
        }

        public static void Render(Storage storage)
        {
            if(Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.C) && !Debug.IsVisible)
            {
                IsVisible = !IsVisible;

                if(IsVisible)
                {
                    Input.ShowCursor();

                    if (Player != null)
                        Player.CanMove = false;


                }
                else
                {
                    Input.HideCursor();

                    if(Player != null)
                        Player.CanMove = true;
                }
            }
            if (!IsVisible) return;

            if (storage == null) return;

            
            ImGuiIOPtr io = ImGui.GetIO();

            var style = ImGui.GetStyle();
            float titleBarHeight = ImGui.GetFontSize() + style.FramePadding.Y * 2;

      

            SlotSize = Math.Clamp(io.DisplaySize.X * 0.04f, 32.0f, 128.0f);
            float windowWidth = (storage.SizeX * SlotSize ) + titleBarHeight;
            float windowHeight = (storage.SizeY * SlotSize ) + titleBarHeight + titleBarHeight;
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


            if (ImGui.BeginTable("InventoryTable", storage.SizeX, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                for (int x = 0; x < storage.SizeX; x++)
                {
                    ImGui.TableSetupColumn($"##column_{x}", ImGuiTableColumnFlags.WidthFixed, SlotSize);
                }
                for (int y = 0; y < storage.SizeY; y++)
                {
                    ImGui.TableNextRow();
                    for (int x = 0; x < storage.SizeX; x++)
                    {
                        ImGui.TableSetColumnIndex(x);
                        ItemSlot slot = storage.GetSlot(x, y);
                        if (slot == null)
                        {
                            continue;
                        }
                        string id = $"slot_{x}_{y}";
                        IntPtr slotTextureId = SlotTexture;

                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));

                        if (slotTextureId == IntPtr.Zero)
                        {
                            if (ImGui.Button("", new Vector2(SlotSize, SlotSize)))
                            {
                                OnSlotClicked(slot);
                            }
                        }
                        else
                        {
                            if (ImGui.ImageButton(id, slotTextureId, new Vector2(SlotSize, SlotSize)))
                            {
                                OnSlotClicked(slot);
                            }
                        }

                        /*if (ImGui.ImageButton(id, iconTextureId, new Vector2(SlotSize, SlotSize)))
                        {
                            OnSlotClicked(slot);
                        }*/

                        if (slot.HasItem)
                        {
                            Vector2 pos = ImGui.GetItemRectMin();


                            Vector2 size = new Vector2(SlotSize, SlotSize) * 0.8f;

                            Vector2 posCenter = ImGui.GetItemRectMin() + new Vector2(SlotSize * 0.5f, SlotSize * 0.5f);

                            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                            drawList.AddImage(GameBlocks.ItemIcon[slot.Item.Id].Handle, posCenter - size * 0.5f, posCenter + size * 0.5f);

                            if (slot.Count > 1)
                            {
                                Vector2 textPos = pos + new Vector2(SlotSize * 0.05f, SlotSize * 0.05f);

                                drawList.AddText(textPos + new Vector2(2, 2), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)),
                                    slot.Count.ToString());
                                drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), 
                                    slot.Count.ToString());
                            }
                        }

                        ImGui.PopStyleColor(3);
                        ImGui.PopStyleVar(3);

                        ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.15f, 0.15f, 0.15f, 1.0f));
                        if (ImGui.IsItemHovered() && slot.HasItem)
                        {

                            var text = "";
                            var type = slot.Item.GetType();

                            if (type == typeof(DrillItem)) 
                            {
                                var itemType = slot.Item as DrillItem;
                                text = "Power: " + itemType.Power;
                            }
                            else if(type == typeof(WeaponItem))
                            {
                                var itemType = slot.Item as WeaponItem;
                                text = "Damage: " + itemType.Damage;
                            }
                            else if (type == typeof(BlockItem))
                            {
                                var itemType = slot.Item as BlockItem;
                                text = "Mass: " + itemType.Mass;
                                text += "\nDurability: " + itemType.Durability;
                            }

                            ImGui.BeginTooltip();
                            ImGui.Text($"Id:{slot.Item.Id}\n{slot.Item.Name}\n" +
                                text);
                            ImGui.EndTooltip();

                        }

                    
                        if (slot.HasItem && slot.Count > 1)
                        {
                            Vector2 cursorPos = ImGui.GetCursorPos();
                            ImGui.SetCursorPos(new Vector2(cursorPos.X + SlotSize - 20, cursorPos.Y + SlotSize - 20));
                            //ImGui.TextColored(new Vector4(1, 1, 1, 1), slot.Count.ToString());
                        }
                    }
                }
                ImGui.EndTable();
            }

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
