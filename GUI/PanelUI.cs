using System;
using System.Numerics;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Extensions;
using Spacebox.Game;

namespace Spacebox.UI
{
    public static class PanelUI
    {
        private static float SlotSize = 64.0f;
        private static IntPtr SlotTexture = IntPtr.Zero;
       // private static IntPtr ItemTexture = IntPtr.Zero;
        private static IntPtr SelectedTexture = IntPtr.Zero;
        public static bool IsVisible { get; set; } = true;



        private static short _selectedSlotId = 0;
        public static short SelectedSlot
        {
            get { return _selectedSlotId; }
            set
            {
                _selectedSlotId = value;
                OnSlotChanged?.Invoke(_selectedSlotId);
            }
        }

        public static Action<short> OnSlotChanged;

        public static void Initialize(IntPtr slotTexture, IntPtr selectedTexture )
        {
            SlotTexture = slotTexture;
            SelectedTexture = selectedTexture;
            //ItemTexture = new Texture2D("Resources/Textures/item.png", true, false).Handle;
        }

        public static void Update()
        {
            if (Input.MouseScrollDelta.Y < 0)
            {
                SelectedSlot++;



                if (SelectedSlot > 9)
                {
                    SelectedSlot = 0;
                }

            }

            if (Input.MouseScrollDelta.Y > 0)
            {
                SelectedSlot--;

                if (SelectedSlot < 0)
                {
                    SelectedSlot = 9;
                }


            }
        }

        public static void Render(Storage storage)
        {
            if (!Settings.ShowInterface) return;
            if (!IsVisible) return;
            if (storage == null) return;

            ImGuiIOPtr io = ImGui.GetIO();
            var style = ImGui.GetStyle();

            // Swap SizeX and SizeY for window dimensions
            SlotSize = Math.Clamp(io.DisplaySize.X * 0.04f, 32.0f, 128.0f);
            float windowWidth = (storage.SizeY * SlotSize);
            float windowHeight = (storage.SizeX * SlotSize) + style.WindowTitleAlign.Y;
            Vector2 displaySize = io.DisplaySize;
            Vector2 windowPos = new Vector2(
                (displaySize.X - windowWidth) / 2,
                (displaySize.Y - windowHeight) * 0.95f
            );

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoMove |
                                           ImGuiWindowFlags.NoTitleBar |
                                           ImGuiWindowFlags.NoScrollbar |
                                           ImGuiWindowFlags.NoScrollWithMouse;

            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.5f, 0.5f, 0.5f, 1f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.5f, 0.5f, 0.5f, 1f));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.5f, 0.5f, 0.5f, 1f));

            ImGui.Begin("Panel", windowFlags);

            if (ImGui.BeginTable("PanelTable", storage.SizeY, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                // Set up columns based on SizeY
                for (int y = 0; y < storage.SizeY; y++)
                {
                    ImGui.TableSetupColumn($"##columnPanel_{y}", ImGuiTableColumnFlags.WidthFixed, SlotSize);
                }

                // Iterate over SizeX for rows
                for (int x = 0; x < storage.SizeX; x++)
                {
                    ImGui.TableNextRow();
                    for (int y = 0; y < storage.SizeY; y++)
                    {
                        ImGui.TableSetColumnIndex(y);

                        // Access slot with swapped indices
                        ItemSlot slot = storage.GetSlot(x,y);
                        if (slot == null)
                        {
                            continue;
                        }
                        string id = $"slotPanel_{y}_{x}";
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
                        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                        Vector2 pos = ImGui.GetItemRectMin();

                        Vector2 sizeSlot = new Vector2(SlotSize, SlotSize);
                        Vector2 sizeItem = sizeSlot * 0.8f;

                        Vector2 posCenter = pos + new Vector2(SlotSize * 0.5f, SlotSize * 0.5f);

                        if (y == SelectedSlot)
                        {
                            drawList.AddImage(SelectedTexture, posCenter - sizeSlot * 0.5f,
                                posCenter + sizeSlot * 0.5f);
                        }
                        if (slot.HasItem)
                        {
                            
                            if(clickedSlot != null)
                            {
                                if(mousePos != Vector2.Zero)
                                {
                                    posCenter = mousePos;
                                }
                            }
                            drawList.AddImage(GameBlocks.ItemIcon[slot.Item.Id].Handle, posCenter - sizeItem * 0.5f, posCenter + sizeItem * 0.5f);

                            

                            if (slot.Count > 1)
                            {
                                Vector2 textPos = pos + new Vector2(SlotSize * 0.05f, SlotSize * 0.05f);
                                drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)),
                                    slot.Count.ToString());
                            }
                        }

                        ImGui.PopStyleColor(3);
                        ImGui.PopStyleVar(3);
                        
                        ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.15f, 0.15f, 0.15f, 1.0f));
                        if (ImGui.IsItemHovered() && slot.HasItem)
                        {
                            
                            

                            ImGui.BeginTooltip();
                            ImGui.Text($"Id:{slot.Item.Id}\n{slot.Item.Name}\n" +
                                $"Count: {slot.Count}");
                            ImGui.EndTooltip();
                      
                        }

                        ImGui.PopStyleColor();
                    }
                }
                ImGui.EndTable();
            }

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar();
            ImGui.End();
        }

        private static Vector2 mousePos = Vector2.Zero;
        private static ItemSlot clickedSlot;

        private static void OnSlotClicked(ItemSlot slot)
        {

            if (slot.HasItem)
            {
                clickedSlot = slot;

                if (Input.IsKey(Keys.LeftControl))
                {

                    slot.MoveItemToConnectedStorage();
                    
                }

                if (Input.IsKey(Keys.LeftAlt))
                {

                    slot.Split();

                }

                if(Input.IsMouseButton(MouseButton.Left))
                {
                    mousePos = Input.Mouse.Position.ToSystemVector2();
                }
            }
            else
            {
                // Handle empty slot click
            }
        }
    }
}
