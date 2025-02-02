﻿using System.Numerics;
using ImGuiNET;
using Microsoft.VisualBasic;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using Engine.Audio;
using Spacebox.Game.GUI.Menu;


namespace Spacebox.Game.GUI
{
    public static class PanelUI
    {
        private static float SlotSize = 64.0f;

        private static nint SlotTexture = nint.Zero;

        // private static IntPtr ItemTexture = IntPtr.Zero;
        private static nint SelectedTexture = nint.Zero;
        public static bool IsVisible { get; set; } = true;
        public static bool AllowScroll { get; set; } = true;

        public static Storage Storage;
        private static ItemModel ItemModel;
        private static ItemSlot SelectedSlot;

        private static AudioSource scrollAudio;

        private static short _selectedSlotId = 0;

        public static short SelectedSlotId
        {
            get { return _selectedSlotId; }
            set { _selectedSlotId = value; }
        }

        public static Action<short> OnSlotChanged;
        private static Shader itemModelShader;

        private static float TimeToHideItemName = 2f;
        private static float _time = 0;
        private static bool wasPlayerOnes = false;

        public static Astronaut Player;

        public static void Initialize(Astronaut player, nint slotTexture, nint selectedTexture)
        {
            Player = player;
            Storage = Player.Panel;
            SlotTexture = slotTexture;
            SelectedTexture = selectedTexture;
            //ItemTexture = new Texture2D("Resources/Textures/item.png", true, false).Handle;
            scrollAudio = new AudioSource(SoundManager.GetClip("scroll"));
            itemModelShader = ShaderManager.GetShader("Shaders/itemModel");
            InventoryUIHelper.SetDefaultIcon(slotTexture, selectedTexture);
            SetSelectedSlot(0);

            Storage.OnDataWasChanged += OnStorageDataWasChanged;


            var inventory = ToggleManager.Register("panel");
            //inventory.IsUI = true;
            inventory.OnStateChanged += s =>
            {
                AllowScroll = s;
            };
        }

        public static OpenTK.Mathematics.Vector2[] GetSelectedBlockUV(Face face, Direction direction)
        {
            if(SelectedSlot == null) return CubeMeshData.GetBasicUVs();

            var blockItem = (SelectedSlot.Item as BlockItem);

            if (blockItem == null) return CubeMeshData.GetBasicUVs();

            var blockID = blockItem.BlockId;

            return GameBlocks.GetBlockUVsByIdAndDirection(blockID, face, direction);
        }

        private static void OnStorageDataWasChanged(Storage storage)
        {
            SelectSlot(SelectedSlotId);
        }

        public static bool IsHolding<T>() where T : Item
        {
            if (SelectedSlot == null) return false;
            if (!SelectedSlot.HasItem) return false;
            //if (InventoryUI.IsVisible) return false;
            return SelectedSlot.Item.Is<T>();
        }

        public static bool IsHoldingConsumable() => IsHolding<ConsumableItem>();
        public static bool IsHoldingDrill() => IsHolding<DrillItem>();
        public static bool IsHoldingWeapon() => IsHolding<WeaponItem>();
        public static bool IsHoldingBlock() => IsHolding<BlockItem>();

        public static void DrawItemModel()
        {
            if (ItemModel == null) return;

            ItemModel.Draw(itemModelShader);
        }

        public static ItemModel GetSlotModel()
        {


            return GameBlocks.ItemModels[SelectedSlot.Item.Id];
        }

        public static bool TryPlaceItem(out short id, GameMode gameMode)
        {
            id = 0;
            if (SelectedSlot == null) return false;
            if (!SelectedSlot.HasItem) return false;
            if (InventoryUI.IsVisible) return false;
            if (!(SelectedSlot.Item is BlockItem itemBlock)) return false;

            id = itemBlock.BlockId;
            if(gameMode == GameMode.Survival)
                SelectedSlot.TakeOne();
            return true;
        }

        private static void ShowItemModel()
        {
            if (SelectedSlot?.HasItem == true && !(SelectedSlot.Item is BlockItem))
            {
                ItemModel = GameBlocks.ItemModels[SelectedSlot.Item.Id];
            }
        }

        private static void HideItemModel()
        {
            if (SelectedSlot == null) return;
            if (!SelectedSlot.HasItem || SelectedSlot.Item is BlockItem)
            {
                ItemModel = null;
            }
        }

        private static void UpdateInput()
        {
            if (Debug.IsVisible) return;
            if (InventoryUI.IsVisible) return;
            if (!AllowScroll) return;

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

           /* if (Input.IsMouseButtonDown(MouseButton.Right))
            {
                if (IsHoldingConsumable())
                {
                    Player.ApplyConsumable((ConsumableItem)SelectedSlot.Item);
                    SelectedSlot.DropOne();
                }
            }*/


            if (Input.IsKeyDown(Keys.G))
            {
                if (SelectedSlot != null)
                {
                    SelectedSlot.DropOne();
                }
            }

            if (Input.IsKeyDown(Keys.D0))
            {
                SetSelectedSlot(9);
            }

            for (int i = 0; i <= 9; i++)
            {
                if (Input.IsKeyDown(Keys.D1 + i % 10))
                {
                    SetSelectedSlot((short)(i % Storage.SizeY));
                    break;
                }
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

        private static void UpdatePlayerInteraction(Astronaut player)
        {
           
            if (player.GameMode == GameMode.Spectator) return;

            if (IsHoldingBlock())
            {
                player.SetInteraction(new InteractionPlaceBlock());
            }
            else if (IsHoldingWeapon())
            {
                player.SetInteraction(new InteractionShoot(SelectedSlot));
            }
            else if (IsHoldingDrill())
            {
                if(player.GameMode == GameMode.Creative)
                {
                    player.SetInteraction(new InteractionDestroyBlock(SelectedSlot));
                }
                else
                {
                    player.SetInteraction(new InteractionDestroyBlockSurvival(SelectedSlot));
                }
                
            }
            else if (IsHoldingConsumable())
            {
                player.SetInteraction(new InteractionConsumeItem(SelectedSlot));
            }
            else if (IsHolding<EraserToolItem>())
            {

                player.SetInteraction(new InteractionEraser());
            }
            else
            {
                player.SetInteraction(new InteractionDefault());
            }
        }

        public static void SetSelectedSlot(short id)
        {
            if (!AllowScroll) return;

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
                SelectedSlot = Storage.GetSlot(0, slot);
                ShowItemModel();
                HideItemModel();
                UpdatePlayerInteraction(Player);

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

            SlotSize = InventoryUIHelper.SlotSize;
            float windowWidth = Storage.SizeY * SlotSize;
            float windowHeight = Storage.SizeX * SlotSize;
            Vector2 displaySize = io.DisplaySize;
            Vector2 windowPos = new Vector2(
                (displaySize.X - windowWidth) / 2,
                (displaySize.Y - windowHeight) * 0.95f
            );

            var padding = SlotSize * 0.05f;
            var paddingV = new Vector2(padding, padding);

            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight ) +paddingV + paddingV);
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoMove |
                                           ImGuiWindowFlags.NoTitleBar |
                                           ImGuiWindowFlags.NoScrollbar |
                                           ImGuiWindowFlags.NoScrollWithMouse
                                           | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing;


            ImGui.Begin("Panel", windowFlags);

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight ) + paddingV + paddingV, displaySize.Y, 0.003f);

            ImGui.SetCursorPos(paddingV );


            if (ImGui.BeginTable("PanelTable", Storage.SizeY, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                //GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), io.DisplaySize.Y);
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
                        ItemSlot slot = Storage.GetSlot(x, y);
                        if (slot == null)
                        {
                            continue;
                        }

                        string id = $"slotPanel_{x}_{y}";
                        nint slotTextureId = SlotTexture;

                        bool isSelected = true && x == 0 && y == SelectedSlotId;
                        InventoryUIHelper.DrawSlot(slot, id, OnSlotClicked, isSelected);


                        InventoryUIHelper.ShowTooltip(slot);

                    }
                }

                ImGui.EndTable();
            }

            ImGui.End();

            if (SelectedSlot != null)
            {
                if (SelectedSlot.HasItem && _time > 0)
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
                                                                  | ImGuiWindowFlags.NoMove |
                                                                  ImGuiWindowFlags.NoResize |
                                                                  ImGuiWindowFlags.NoScrollbar |
                                                                  ImGuiWindowFlags.NoScrollWithMouse |
                                                                  ImGuiWindowFlags.NoInputs);


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

        public static ItemSlot CurrentSlot()
        {
            return SelectedSlot;
        }

        private static void OnSlotClicked(ItemSlot slot)
        {
            if (Input.IsMouseButton(MouseButton.Left))
            {
            
            }

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