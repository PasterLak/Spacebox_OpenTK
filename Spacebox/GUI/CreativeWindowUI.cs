using System.Numerics;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Game;
using Spacebox.Game.Player;
using Spacebox.GUI;

namespace Spacebox.UI
{
    public static class CreativeWindowUI
    {
        private static float SlotSize = 64.0f;
        private static IntPtr SlotTexture = IntPtr.Zero;
      
        public static bool IsVisible { get; set; } = false;
        public static bool Enabled { get; set; } = false;

        public static Astronaut Player;

        public static Storage storage;

        public static void SetDefaultIcon(IntPtr textureId)
        {
            SlotTexture = textureId;

            storage = GameBlocks.CreateCreativeStorage(5);
        }

        public static void Render()
        {
            if (!Enabled) return;
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.C) && !Debug.IsVisible)
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
            if (!IsVisible) return;

            if (storage == null) return;


            ImGuiIOPtr io = ImGui.GetIO();

            var style = ImGui.GetStyle();
            float titleBarHeight = ImGui.GetFontSize() + style.FramePadding.Y * 2;



            SlotSize = Math.Clamp(io.DisplaySize.X * 0.04f, 32.0f, 128.0f);

            float windowWidth = (5 * SlotSize) + titleBarHeight + titleBarHeight;
            float windowHeight = (7 * SlotSize) + titleBarHeight + titleBarHeight;

            Vector2 displaySize = io.DisplaySize;
            Vector2 windowPos = new Vector2(
                (displaySize.X - windowWidth) / 5f,
                (displaySize.Y - windowHeight) / 2
            );
            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoCollapse
                                          //ImGuiWindowFlags.NoMove |

                                          //ImGuiWindowFlags.NoScrollbar |
                                          //ImGuiWindowFlags.NoScrollWithMouse
                                          ;

            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.5f, 0.5f, 0.5f, 1f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.5f, 0.5f, 0.5f, 1f));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.5f, 0.5f, 0.5f, 1f));

            ImGui.Begin("Creative", windowFlags);

            //ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            //ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(15, 15));

            //ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(2,2));


            if (ImGui.BeginTable("CreativeTable", storage.SizeX, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                for (int x = 0; x < storage.SizeX; x++) 
                {
                    ImGui.TableSetupColumn($"##columnCreative_{x}", ImGuiTableColumnFlags.WidthFixed, SlotSize);
                }
                for (int y = 0; y < storage.SizeY; y++)
                {
                    ImGui.TableNextRow();
                    for (int x = 0; x < storage.SizeX; x++)
                    {
                        ImGui.TableSetColumnIndex(x);

                        ItemSlot slot = storage.GetSlot(x,y);
                        if (slot == null)
                        {
                            continue;
                        }
                        string id = $"slotCreative_{x}_{y}";
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

                        if (slot.HasItem)
                        {
                            Vector2 pos = ImGui.GetItemRectMin();
                            Vector2 size = new Vector2(SlotSize, SlotSize) * 0.8f;
                            Vector2 posCenter = pos + new Vector2(SlotSize * 0.5f, SlotSize * 0.5f);

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
                      
                        ImGui.PopStyleVar();

                        InventoryUIHelper.ShowTooltip(slot, true);


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
                if(Player != null)
                {
                    if(Input.IsKey(Keys.LeftShift))
                    {
                        Player.Panel.TryAddItem(slot.Item, slot.Item.StackSize);
                    }
                    else if (Input.IsKey(Keys.LeftControl))
                    {
                        Player.Panel.TryAddItem(slot.Item, (byte)(slot.Item.StackSize / 2));
                    }
                    else
                    {
                        Player.Panel.TryAddItem(slot.Item, 1);
                    }
                    
                }
            }
            else
            {
            }
        }
    }
}
