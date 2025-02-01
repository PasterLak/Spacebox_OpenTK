using System.Numerics;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Engine;
using Spacebox.Engine;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI
{
    public static class CreativeWindowUI
    {
        private static float SlotSize = 64.0f;
        private static nint SlotTexture = nint.Zero;

        public static bool IsVisible { get; set; } = false;
        public static bool Enabled { get; set; } = false;

        private static Astronaut player;

        private static Storage storage;

        public static void SetDefaultIcon(nint textureId, Astronaut player)
        {
            SlotTexture = textureId;
            CreativeWindowUI.player = player;
            storage = GameBlocks.CreateCreativeStorage(5);

            var inventory = ToggleManager.Register("creative");
            inventory.IsUI = true;
            inventory.OnStateChanged += s =>
            {
                IsVisible = s;
            };
        }

        public static void Render()
        {
            if (!Enabled) return;
            if (!IsVisible) return;

            if (storage == null) return;


            ImGuiIOPtr io = ImGui.GetIO();

            float titleBarHeight = ImGui.GetFontSize();


            SlotSize = InventoryUIHelper.SlotSize;

            float windowWidth = 5 * SlotSize ;
            float windowHeight = 7 * SlotSize + titleBarHeight + titleBarHeight;

            Vector2 displaySize = io.DisplaySize;
            Vector2 windowPos = new Vector2(
                (displaySize.X - windowWidth) / 7f,
                (displaySize.Y - windowHeight) / 2
            );

            var padding = SlotSize * 0.1f;
            var paddingV = new Vector2(padding, padding);

            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight + padding * 4) + paddingV + paddingV);
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoDecoration |
                                          ImGuiWindowFlags.NoMove 

                                          //ImGuiWindowFlags.NoScrollbar |
                                          //ImGuiWindowFlags.NoScrollWithMouse
                                          ;

        
            ImGui.Begin("Creative", windowFlags);

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight + padding * 4) + paddingV + paddingV, displaySize.Y);

            ImGui.SetCursorPos(paddingV);
            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1f), "Creative");

            ImGui.SetCursorPos(paddingV + new Vector2(0, padding * 4));

            ImGui.PushStyleColor(ImGuiCol.Button, Theme.Colors.Deep);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
            //ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(1, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 0, 0, 0));


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

                        ItemSlot slot = storage.GetSlot(x, y);
                        if (slot == null)
                        {
                            continue;
                        }
                        string id = $"slotCreative_{x}_{y}";
                        nint slotTextureId = SlotTexture;


                        
                        if (slotTextureId == nint.Zero)
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

                        
                        InventoryUIHelper.ShowTooltip(slot, true);

                        

                    }
                }
                ImGui.PopStyleColor(4);
                ImGui.EndTable();
            }

            ImGui.End();
        }

        private static void OnSlotClicked(ItemSlot slot)
        {
            if (slot.HasItem)
            {
                if (player != null)
                {
                    if (Input.IsKey(Keys.LeftShift))
                    {
                        player.Panel.TryAddItem(slot.Item, slot.Item.StackSize);
                    }
                    else if (Input.IsKey(Keys.LeftControl))
                    {
                        player.Panel.TryAddItem(slot.Item, (byte)(slot.Item.StackSize / 2));
                    }
                    else
                    {
                        player.Panel.TryAddItem(slot.Item, 1);
                    }

                }
            }
            else
            {
            }
        }
    }
}
