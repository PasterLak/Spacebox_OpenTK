using System;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Game;

namespace Spacebox.UI
{
    public static class InventoryUIHelper
    {
        public static float SlotSize = 64.0f;
        public static IntPtr SlotTexture = IntPtr.Zero;
        public static IntPtr ItemTexture = IntPtr.Zero;
        public static IntPtr SelectedTexture = IntPtr.Zero;

        public static void SetDefaultIcon(IntPtr slotTextureId, IntPtr selectedTextureId)
        {
            SlotTexture = slotTextureId;
            SelectedTexture = selectedTextureId;
            ItemTexture = new Texture2D("Resources/Textures/item.png", true, false).Handle;

            Window.OnResized += OnResize;

            OnResize(Window.Instance.Size);
        }

        private static void OnResize(OpenTK.Mathematics.Vector2 w)
        {
            SlotSize = Math.Clamp(w.X * 0.04f, 32.0f, 128.0f);
        }

        public static void RenderStorage(Storage storage, Action<ItemSlot> onSlotClicked, int columns, bool isPanel = false, short selectedSlotId = -1)
        {

            ImGuiIOPtr io = ImGui.GetIO();

            //SlotSize = Math.Clamp(io.DisplaySize.X * 0.04f, 32.0f, 128.0f);

            if (ImGui.BeginTable("StorageTable", columns, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                for (int x = 0; x < columns; x++)
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
                        bool isSelected = isPanel && x == 0 && y == selectedSlotId;
                        DrawSlot(slot, id, onSlotClicked, isSelected);
                        ShowTooltip(slot);
                    }
                }
                ImGui.EndTable();
            }
        }

        private static bool IsDragging = false;
        private static ItemSlot startSlot = null;
        private static Storage startStorage = null;
        public static unsafe void DrawSlot(ItemSlot slot, string id, Action<ItemSlot> onSlotClicked, bool isSelected = false)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.5f, 0.5f, 0.5f, 0.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(1, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 0, 0, 0));
            if (SlotTexture == IntPtr.Zero)
            {
                if (ImGui.Button("", new Vector2(SlotSize, SlotSize)))
                {
                    onSlotClicked(slot);
                }
            }
            else
            {
                if (ImGui.ImageButton(id, SlotTexture, new Vector2(SlotSize, SlotSize)))
                {
                    onSlotClicked(slot);
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
               
                OnSlotRightClicked(slot);
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
               
                OnSlotClicked(slot);
            }

            if (!ImGui.IsItemActive() && ImGui.IsItemHovered())
            {
               // Debug.Log("Right released!");
            }
           

            if (slot.HasItem )
            {
               
                if ( ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                {
                    short slotId = slot.SlotId;
                    byte[] bytes = BitConverter.GetBytes(slotId);
                    startStorage = slot.Storage;
                    startSlot = slot;
                    IsDragging = true;
                    fixed (byte* pBytes = bytes)
                    {
                        ImGui.SetDragDropPayload("ITEM_SLOT", (IntPtr)pBytes, sizeof(short));
                    }

                    ShowDragPreview(slot);


                    ImGui.EndDragDropSource();
                }
            }
       
            if (ImGui.BeginDragDropTarget())
            {
                ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("ITEM_SLOT", ImGuiDragDropFlags.None);
                if (payload.NativePtr != null)
                {
            
                    short sourceSlotId = Marshal.ReadInt16(payload.Data);

                    if(startStorage != null && startSlot != null)
                    {
                    
                        if(startSlot != slot)
                        startSlot.SwapWith(slot);

                      
                    }
                   
                }
                IsDragging = false;

                ImGui.EndDragDropTarget();
            }



            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetItemRectMin();

            Vector2 sizeSlot = new Vector2(SlotSize, SlotSize);
            Vector2 sizeItem = sizeSlot * 0.8f;

            Vector2 posCenter = pos + new Vector2(SlotSize * 0.5f, SlotSize * 0.5f);

            if (isSelected && SelectedTexture != IntPtr.Zero)
            {
                drawList.AddImage(SelectedTexture, posCenter - sizeSlot * 0.5f, posCenter + sizeSlot * 0.5f);
            }

            if (slot.HasItem)
            {
                drawList.AddImage(GameBlocks.ItemIcon[slot.Item.Id].Handle, posCenter - sizeItem * 0.5f, posCenter + sizeItem * 0.5f);

                if (slot.Count > 1)
                {
                    Vector2 textPos = pos + new Vector2(SlotSize * 0.05f, SlotSize * 0.05f);

                    drawList.AddText(textPos + new Vector2(2, 2), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)),
                        slot.Count.ToString());
                    drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)),
                        slot.Count.ToString());
                }
            }

            ImGui.PopStyleColor(5);
        }

        private static void ShowDragPreview(ItemSlot slot)
        {
          
         
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetItemRectMin();

            if (GameBlocks.ItemIcon.ContainsKey(slot.Item.Id))
            {
                ImGui.Image(GameBlocks.ItemIcon[slot.Item.Id].Handle, new Vector2(SlotSize * 0.8f, SlotSize * 0.8f));
            }
            if (slot.Count > 1)
            {
                Vector2 textPos = pos + new Vector2(SlotSize * 0.05f, SlotSize * 0.05f);

               
                drawList.AddText(textPos + new Vector2(2, 2), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)),
                    slot.Count.ToString());
                drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)),
                    slot.Count.ToString());
            }
         
        }

        public static void ShowTooltip(ItemSlot slot, bool showStackSize = false)
        {
            if (IsDragging) return;
            if (ImGui.IsItemHovered() && slot.HasItem)
            {
                var text = "";
                var type = slot.Item.GetType();

                if(showStackSize)
                {
                    if(slot.Item.StackSize > 1)
                    {
                        text += "Stack: " + slot.Item.StackSize;
                    }
                }

                if (type == typeof(DrillItem))
                {
                    var itemType = slot.Item as DrillItem;
                    text += "\nPower: " + itemType.Power;
                }
                else if (type == typeof(WeaponItem))
                {
                    var itemType = slot.Item as WeaponItem;
                    text += "\nDamage: " + itemType.Damage;
                }
                else if (type == typeof(BlockItem))
                {
                    var itemType = slot.Item as BlockItem;
                    text += "\nMass: " + itemType.Mass;
                    text += "\nDurability: " + itemType.Durability;

                }
                else if (type == typeof(ConsumableItem))
                {
                    var itemType = slot.Item as ConsumableItem;
                    text = "Healing: +" + itemType.HealAmount;
                

                }

                ImGui.BeginTooltip();
                ImGui.Text($"Id:{slot.Item.Id}\n{slot.Item.Name}\n{text}");
                ImGui.EndTooltip();
            }
        }


        private static void OnSlotRightClicked(ItemSlot slot)
        {


            if (slot.HasItem)
            {
                slot.Split();
            }
         
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

        public static void DrawItemName(string name, float timeToHide)
        {
            if (timeToHide <= 0) return;

            ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always);
            ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
            ImGui.Begin("ItemName", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoInputs);

            Vector2 textSize = ImGui.CalcTextSize(name);
            float posX = (ImGui.GetWindowWidth() - textSize.X) * 0.5f;
            float posY = (ImGui.GetWindowHeight() - textSize.Y) * 0.98f;

            ImGui.SetCursorPos(new Vector2(posX + 3, posY + 3));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0, 0, 1));
            ImGui.TextUnformatted(name);
            ImGui.PopStyleColor();

            ImGui.SetCursorPos(new Vector2(posX, posY));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));
            ImGui.TextUnformatted(name);
            ImGui.PopStyleColor();

            ImGui.End();
        }
    }
}
