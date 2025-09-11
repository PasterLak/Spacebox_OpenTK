using System.Numerics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Engine;
using Spacebox.Game.Player;
using Engine.Audio;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Player.Interactions;
using Spacebox.Game.Player.GameModes;


namespace Spacebox.Game.GUI
{
    public static class PanelUI
    {
        private static float SlotSize = 64.0f;
        private static nint SlotTexture = nint.Zero;
        private static nint SelectedTexture = nint.Zero;
        public static bool IsVisible { get; set; } = true;
        public static bool EnableRenderForCurrentItem { get; set; } = true;
        public static bool AllowScroll { get; set; } = true;
        public static bool IsItemModelVisible { get; set; } = true;
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

        private static float TimeToHideItemName = 2f;
        private static float _time = 0;
        private static bool wasPlayerOnes = false;
        public static Astronaut Player;
        private static short _lastSelectedSlotId = -1;
        private static Item _lastSelectedItem = null;
        private static byte _lastSelectedCount = 0;

        public static void Initialize(Astronaut player, nint slotTexture, nint selectedTexture)
        {
            Player = player;
            Storage = Player.Panel;
            SlotTexture = slotTexture;
            SelectedTexture = selectedTexture;
            scrollAudio = new AudioSource(Resources.Load<AudioClip>("scroll"));

            InventoryUIHelper.SetDefaultIcon(slotTexture, selectedTexture);
            SetSelectedSlot(0);
            Storage.OnDataWasChanged += OnStorageDataWasChanged;
            var inventory = ToggleManager.Register("panel");
            inventory.OnStateChanged += s => { 
                AllowScroll = s; 

                if(s)
                {
                    ShowItemDescription();
                }
            };
            _lastSelectedItem = null;
            _lastSelectedSlotId = -1;
            _lastSelectedCount = 0;
        }

         

        private static void OnStorageDataWasChanged(Storage storage)
        {
            SelectSlot(SelectedSlotId);
        }

        public static bool IsHolding<T>() where T : Item
        {
            if (SelectedSlot == null) return false;
            if (!SelectedSlot.HasItem) return false;
            return SelectedSlot.Item.Is<T>();
        }

        public static void DrawItemModel()
        {
            if (!IsItemModelVisible) return;
            if (ItemModel == null) return;
            ItemModel.Render();
        }

        public static ItemModel GetSlotModel()
        {
            return GameAssets.ItemModels[SelectedSlot.Item.Id];
        }

        public static bool TryPlaceItem(out short id, GameMode gameMode)
        {
            id = 0;
            if (SelectedSlot == null) return false;
            if (!SelectedSlot.HasItem) return false;
            if (InventoryUI.IsVisible) return false;
            if (!(SelectedSlot.Item is BlockItem itemBlock)) return false;
            id = itemBlock.BlockId;
            if (gameMode == GameMode.Survival)
                SelectedSlot.TakeOne();
            return true;
        }

        public static void ShowItemModel()
        {

            bool hadModel = ItemModel != null;
            var lastModel = ItemModel;
            if (SelectedSlot?.HasItem == true && !(SelectedSlot.Item is BlockItem))
            {
                ItemModel = GameAssets.ItemModels[SelectedSlot.Item.Id];

                if (!hadModel) ItemModel.PlayDrawAnimation();
                else
                {
                    if (lastModel != ItemModel)
                    {
                        ItemModel.PlayDrawAnimation();
                    }
                }
            }


        }

        private static void HideItemModel()
        {
            if (SelectedSlot == null) return;
            if (!SelectedSlot.HasItem || SelectedSlot.Item is BlockItem)
            {
                if (ItemModel != null)
                    ItemModel.ResetToEnd();

                ItemModel = null;
            }

        }

        private static void UpdateInput()
        {
            if (Debug.IsVisible) return;
            if (InventoryUI.IsVisible) return;
            if (!AllowScroll) return;
            if (Player.CanMove == false) return;

            if (Input.MouseScrollDelta.Y < 0)
            {
                SelectedSlotId++;
                if (SelectedSlotId > (short)(Storage.SizeY - 1))
                    SelectedSlotId = 0;
                SetSelectedSlot(SelectedSlotId);
            }
            if (Input.MouseScrollDelta.Y > 0)
            {
                SelectedSlotId--;
                if (SelectedSlotId < 0)
                    SelectedSlotId = (short)(Storage.SizeY - 1);
                SetSelectedSlot(SelectedSlotId);
            }
            if (Input.IsActionDown("dropItem"))
            {
                DropItem(SelectedSlot);
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

        private static void DropItem(ItemSlot slot)
        {
            if (slot == null) return;
            if (!slot.HasItem) return;
            if (Player == null) return;
            if (Player.GameMode == GameMode.Spectator) return;

            var dropPosition = Player.Position + Player.Front * 0.5f;

            slot.DropOne();
        }

        public static void Update()
        {
            if (_time > 0)
                _time -= Time.Delta;

            if (ItemModel != null && EnableRenderForCurrentItem)
            {
                if (ItemModel.Enabled)
                {
                    ItemModel.EnableSway = Player.CanMove;
                    ItemModel.Update();

                }

            }

            if (InventoryUI.IsVisible) return;
            UpdateInput();



        }

        private static void UpdatePlayerInteraction(Astronaut player)
        {
            if (player == null) return;
            if (player.GameMode == GameMode.Spectator) return;
            if (IsHolding<BlockItem>())
                player.SetInteraction(new InteractionPlaceBlock());
            else if (IsHolding<WeaponItem>())
                player.SetInteraction(new InteractionShoot(SelectedSlot));
            else if (IsHolding<DrillItem>())
            {
                if (player.GameMode == GameMode.Creative)
                    player.SetInteraction(new InteractionDestroyBlockCreative(SelectedSlot));
                else
                    player.SetInteraction(new InteractionDestroyBlockSurvival(SelectedSlot));
            }
            else if (IsHolding<ConsumableItem>())
                player.SetInteraction(new InteractionConsumeItem(SelectedSlot));
            else if (IsHolding<EraserToolItem>())
                player.SetInteraction(new InteractionEraser());
            else if (IsHolding<CameraPointItem>())
                player.SetInteraction(new InteractionCameraPoint(ItemModel));
            else
                player.SetInteraction(new InteractionDefault());
        }

        public static void ResetLastSelected()
        {
            _lastSelectedSlotId = -1;
            _lastSelectedCount = 0;
            _lastSelectedItem = null;
        }
        public static void SetSelectedSlot(short id)
        {
            // if (!AllowScroll) return;
            SelectedSlotId = id;
            SelectSlot(SelectedSlotId);
            if (wasPlayerOnes)
            {
                if (scrollAudio != null)
                {
                    if (scrollAudio.IsPlaying)
                        scrollAudio.Stop();
                    scrollAudio.Play();
                }

            }
            else
            {
                wasPlayerOnes = true;
            }
        }

        public static void SetFlashlight(Astronaut ast)
        {
            if(ast != null)
            {
                if(ItemModel != null)
                {
                    ItemModel.Material.SetFlashlight(ast.Flashlight);
                }
            }
        }

        private static void SelectSlot(short slot)
        {
            if (Storage != null)
            {
                SelectedSlot = Storage.GetSlot(0, slot);

                var hadModel = ItemModel != null;

                ShowItemModel();
                HideItemModel();



                if (_lastSelectedSlotId != slot ||
                    _lastSelectedItem != SelectedSlot.Item ||
                    _lastSelectedCount != SelectedSlot.Count)
                {
                    UpdatePlayerInteraction(Player);
                    _lastSelectedSlotId = slot;
                    _lastSelectedItem = SelectedSlot.Item;
                    _lastSelectedCount = SelectedSlot.Count;
                }

                if(Player != null && SelectedSlot != null)
                Player.SetItemLight(SelectedSlot);


                ShowItemDescription();



                    OnSlotChanged?.Invoke(slot);
            }
        }

        private static void ShowItemDescription()
        {
            if (!ToggleManager.IsActiveAndExists("inventory"))
            {

                if (SelectedSlot.HasItem && SelectedSlot.Item != null)
                {
                    _time = TimeToHideItemName;


                    if (SelectedSlot.Item.Description != "")
                    {
                        ItemControlsUI.IsVisible = true;
                        ItemControlsUI.Text = SelectedSlot.Item.Description;
                    }
                    else
                    {
                        ItemControlsUI.IsVisible = false;
                    }

                }
                else
                {
                    ItemControlsUI.IsVisible = false;
                }
            }
        }

        public static void Render()
        {
            PanelRenderer.Render(Storage, SlotSize, SlotTexture, SelectedTexture, SelectedSlotId, _time);

            if (Settings.ShowInterface)
            {
                if (SelectedSlot != null && SelectedSlot.HasItem && _time > 0)
                    PanelRenderer.DrawItemName(SelectedSlot.Item.Name);
            }

        }

        public static ItemSlot CurrentSlot() => SelectedSlot;

        public static void OnSlotClicked(ItemSlot slot)
        {
            if (Input.IsMouseButton(MouseButton.Left))
            {
            }
            if (slot.HasItem)
            {
                if (Input.IsAction("storage_item_quick_transfer"))
                    slot.MoveItemToConnectedStorage();
                if (Input.IsKey(Keys.LeftAlt))
                    slot.Split();
                if (Input.IsAction("storage_item_delete"))
                    slot.Clear();
                if (Input.IsMouseButton(MouseButton.Left))
                {
                    Vector2 mousePos = Input.Mouse.Position.ToSystemVector2();
                }
            }
        }
    }


}
