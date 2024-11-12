using System;
using System.Numerics;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Extensions;
using Spacebox.Game;
using Spacebox.Scenes;

namespace Spacebox.UI
{
    public static class PanelUI
    {
        private static float SlotSize = 64.0f;
        private static IntPtr SlotTexture = IntPtr.Zero;
       // private static IntPtr ItemTexture = IntPtr.Zero;
        private static IntPtr SelectedTexture = IntPtr.Zero;
        public static bool IsVisible { get; set; } = true;

        public static Storage Storage;
        private static ItemModel ItemModel;
        private static ItemSlot SelectedSlot;

        private static AudioSource scrollAudio;

        private static short _selectedSlotId = 0;
        public static short SelectedSlotId
        {
            get { return _selectedSlotId; }
            set
            {
                _selectedSlotId = value;
               
            }
        }

        public static Action<short> OnSlotChanged;
        private static Shader itemModelShader;

        private static float TimeToHideItemName = 2f;
        private static float _time = 0;
        private static bool wasPlayerOnes = false;
        public static void Initialize(Storage storage, IntPtr slotTexture, IntPtr selectedTexture )
        {
            Storage = storage;
            SlotTexture = slotTexture;
            SelectedTexture = selectedTexture;
            //ItemTexture = new Texture2D("Resources/Textures/item.png", true, false).Handle;
            scrollAudio = new AudioSource(SoundManager.GetClip("scroll"));
            itemModelShader = ShaderManager.GetShader("Shaders/itemModel");
            SetSelectedSlot(0);

            Storage.OnDataWasChanged += OnStorageDataWasChanged;

            
        }

        public static OpenTK.Mathematics.Vector2[] GetSelectedBlockUV(Face face, Direction direction)
        {
            
                var blockID = (SelectedSlot.Item as BlockItem).BlockId;


            return GameBlocks.GetBlockUVsByIdAndDirection(blockID, face, direction);
           
        }

        private static void OnStorageDataWasChanged(Storage storage)
        {
           
            SelectSlot(SelectedSlotId);
        }

        public static void DrawItemModel()
        {
            if (ItemModel == null) return;

            
             ItemModel.Draw(itemModelShader);
            
        }

        public static bool IsHoldingConsumable()
        {
            if (SelectedSlot == null) return false;

            if (!SelectedSlot.HasItem) return false;

            //if (InventoryUI.IsVisible) return false;

            if (SelectedSlot.Item.GetType() != typeof(ConsumableItem)) return false;


            return true;
        }

        public static bool IsHoldingDrill()
        {
            if (SelectedSlot == null) return false;

            if (!SelectedSlot.HasItem) return false;

            //if (InventoryUI.IsVisible) return false;

            if (SelectedSlot.Item.GetType() != typeof(DrillItem)) return false;
            

            return true;
        }

        public static bool IsHoldingBlock()
        {
            if (SelectedSlot == null) return false;

            if (!SelectedSlot.HasItem) return false;

            //if (InventoryUI.IsVisible) return false;

            if (SelectedSlot.Item.GetType() != typeof(BlockItem)) return false;


            return true;
        }

        public static void SetItemColor(Vector3 color)
        {
            if (ItemModel == null) return;

            ItemModel.SetColor(color.ToOpenTKVector3());
        }

        public static bool TryPlaceItem(out short id)
        {
            id = 0;

            if (SelectedSlot == null) return false;

            if(!SelectedSlot.HasItem) return false;

            if(InventoryUI.IsVisible) return false;

            if(SelectedSlot.Item.GetType() != typeof(BlockItem)) return false;

           
            BlockItem itemBlock = (BlockItem)SelectedSlot.Item;

            if(itemBlock == null) return false;

            id = itemBlock.BlockId;

            SelectedSlot.TakeOne();

            return true;
        }

      
        private static void ShowItemModel()
        {
            if (SelectedSlot == null) return;
            
            if(!SelectedSlot.HasItem) return;

            if(SelectedSlot.Item.GetType() == typeof(BlockItem)) return;

            ItemModel = GameBlocks.ItemModels[SelectedSlot.Item.Id];
        }

        private static void HideItemModel()
        {
            if (SelectedSlot == null) return;

            if(!SelectedSlot.HasItem)
            {
                ItemModel = null;
                return;
            }

            if (SelectedSlot.Item.GetType() == typeof(BlockItem))
            {
                ItemModel = null;
            }

        }

        private static void UpdateInput()
        {
            if (Debug.IsVisible) return;
            if (InventoryUI.IsVisible) return;

            if (Input.MouseScrollDelta.Y < 0)
            {
                SelectedSlotId++;



                if (SelectedSlotId > (short)(Storage.SizeY - 1))
                {
                    SelectedSlotId = 0;
                }
                SetSelectedSlot(SelectedSlotId);
            }

            if (Input.MouseScrollDelta.Y > 0)
            {
                SelectedSlotId--;

                if (SelectedSlotId < 0)
                {
                    SelectedSlotId = (short)(Storage.SizeY - 1);
                }

                SetSelectedSlot(SelectedSlotId);
            }
            if (Input.IsMouseButtonDown(MouseButton.Right))
            {
                if(IsHoldingConsumable())
                {
                    SelectedSlot.DropOne();
                }
            }


                if (Input.IsKeyDown(Keys.G))
            {
                if (SelectedSlot != null)
                {
                    SelectedSlot.DropOne();
                }
            }

            if (Input.IsKeyDown(Keys.D1))
                {
                    SetSelectedSlot(0);
                }
                if (Input.IsKeyDown(Keys.D2))
                {
                    SetSelectedSlot(1);
                }
                if (Input.IsKeyDown(Keys.D3))
                {
                    SetSelectedSlot(2);
                }
                if (Input.IsKeyDown(Keys.D4))
                {
                    SetSelectedSlot(3);
                }
                if (Input.IsKeyDown(Keys.D5))
                {
                    SetSelectedSlot(4);
                }
                if (Input.IsKeyDown(Keys.D6))
                {
                    SetSelectedSlot(5);
                }
                if (Input.IsKeyDown(Keys.D7))
                {
                    SetSelectedSlot(6);
                }
                if (Input.IsKeyDown(Keys.D8))
                {
                    SetSelectedSlot(7);
                }
                if (Input.IsKeyDown(Keys.D9))
                {
                    SetSelectedSlot(8);
                }
                if (Input.IsKeyDown(Keys.D0))
                {
                    SetSelectedSlot(9);
                }
            
        }

        public static void Update()
        {
            if (_time > 0)
            {
                _time -= Time.Delta;
            }
            if (InventoryUI.IsVisible) return;


            UpdateInput();

        }

        private static void SetSelectedSlot(short id)
        {
            SelectedSlotId = id;
            SelectSlot(id);

            if (wasPlayerOnes)
            {
                if (scrollAudio.IsPlaying)
                {
                    scrollAudio.Stop();
                }
                scrollAudio.Play();
            }
            else
            {
                wasPlayerOnes = true;
            }
        }
        private static void SelectSlot(short slot)
        {
            if (Storage != null)
            {

                SelectedSlot = Storage.GetSlot(0,slot);
                ShowItemModel();
                HideItemModel();

                if (SelectedSlot.HasItem)
                {
                    _time = TimeToHideItemName;
                }

                OnSlotChanged?.Invoke(slot);

                
            }
        }

        public static void Render()
        {
            if (!Settings.ShowInterface) return;
            if (!IsVisible) return;
            if (Storage == null) return;


            ImGuiIOPtr io = ImGui.GetIO();
            var style = ImGui.GetStyle();

            // Swap SizeX and SizeY for window dimensions
            SlotSize = Math.Clamp(io.DisplaySize.X * 0.04f, 32.0f, 128.0f);
            float windowWidth = (Storage.SizeY * SlotSize);
            float windowHeight = (Storage.SizeX * SlotSize) + style.WindowTitleAlign.Y;
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

            if (ImGui.BeginTable("PanelTable", Storage.SizeY, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                // Set up columns based on SizeY
                for (int y = 0; y < Storage.SizeY; y++)
                {
                    ImGui.TableSetupColumn($"##columnPanel_{y}", ImGuiTableColumnFlags.WidthFixed, SlotSize);
                }

                // Iterate over SizeX for rows
                for (int x = 0; x < Storage.SizeX; x++)
                {
                    ImGui.TableNextRow();
                    for (int y = 0; y < Storage.SizeY; y++)
                    {
                        ImGui.TableSetColumnIndex(y);

                        // Access slot with swapped indices
                        ItemSlot slot = Storage.GetSlot(x,y);
                        if (slot == null)
                        {
                            continue;
                        }
                        string id = $"slotPanel_{x}_{y}";
                        IntPtr slotTextureId = SlotTexture;

                        //ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.5f, 0.5f, 0.5f, 0.5f));
                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.5f, 0.5f, 0.5f, 0.0f));
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

                        if (y == SelectedSlotId)
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
                            else if (type == typeof(WeaponeItem))
                            {
                                var itemType = slot.Item as WeaponeItem;
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

                        ImGui.PopStyleColor();
                    }
                }
                ImGui.EndTable();
            }

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar();
            ImGui.End();

            if (SelectedSlot != null)
            {
                if(SelectedSlot.HasItem && _time > 0)
                {
                    DrawItemName(SelectedSlot.Item.Name);
                }
                
            }
        }

        private static readonly Vector4 colorWhite = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private static readonly Vector4 colorBlack = new Vector4(0, 0, 0, 1);
        private static void DrawItemName(string name)
        {
            ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always, new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(ImGui.GetIO().DisplaySize.X, ImGui.GetIO().DisplaySize.Y));
            ImGui.Begin("ItemName", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoInputs);


            Vector2 textSize = ImGui.CalcTextSize(name);


            float posX = (ImGui.GetWindowWidth() - textSize.X) * 0.5f;
            float posY = (ImGui.GetWindowHeight() - textSize.Y) * 0.98f;

            ImGui.SetCursorPos(new Vector2(posX + 3, posY + 3));
            ImGui.PushStyleColor(ImGuiCol.Text, colorBlack);
            ImGui.TextUnformatted(name);
            ImGui.PopStyleColor();
            ImGui.SetCursorPos(new Vector2(posX, posY));
            ImGui.PushStyleColor(ImGuiCol.Text, colorWhite);
            ImGui.TextUnformatted(name);

            
            ImGui.PopStyleColor();

            ImGui.End();
        }

        private static Vector2 mousePos = Vector2.Zero;
        private static ItemSlot clickedSlot;

        private static void OnSlotClicked(ItemSlot slot)
        {

            if (slot.HasItem)
            {
                clickedSlot = slot;

                if (Input.IsKey(Keys.LeftShift))
                {

                    slot.MoveItemToConnectedStorage();
                    
                }

                if (Input.IsKey(Keys.LeftAlt))
                {

                    slot.Split();

                }

                if (Input.IsKey(Keys.X))
                {

                    slot.Clear();

                }

                if (Input.IsMouseButton(MouseButton.Left))
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
