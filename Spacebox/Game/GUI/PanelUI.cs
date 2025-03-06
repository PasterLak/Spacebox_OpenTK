using System.Numerics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using Engine.Audio;


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
            scrollAudio = new AudioSource(SoundManager.GetClip("scroll"));
         
            InventoryUIHelper.SetDefaultIcon(slotTexture, selectedTexture);
            SetSelectedSlot(0);
            Storage.OnDataWasChanged += OnStorageDataWasChanged;
            var inventory = ToggleManager.Register("panel");
            inventory.OnStateChanged += s => { AllowScroll = s; };
            _lastSelectedItem = null;
            _lastSelectedSlotId = -1;
            _lastSelectedCount = 0;
        }

        public static OpenTK.Mathematics.Vector2[] GetSelectedBlockUV(Face face, Direction direction)
        {
            if (SelectedSlot == null) return CubeMeshData.GetBasicUVs();
            var blockItem = SelectedSlot.Item as BlockItem;
            if (blockItem == null) return CubeMeshData.GetBasicUVs();
            return GameAssets.GetBlockUVsByIdAndDirection(blockItem.BlockId, face, direction);
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

        private static void ShowItemModel()
        {
            if (SelectedSlot?.HasItem == true && !(SelectedSlot.Item is BlockItem))
                ItemModel = GameAssets.ItemModels[SelectedSlot.Item.Id];
        }

        private static void HideItemModel()
        {
            if (SelectedSlot == null) return;
            if (!SelectedSlot.HasItem || SelectedSlot.Item is BlockItem)
                ItemModel = null;
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
            if (Input.IsKeyDown(Keys.G))
            {
                if (SelectedSlot != null)
                    SelectedSlot.DropOne();
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
                _time -= Time.Delta;
            if (InventoryUI.IsVisible) return;
            UpdateInput();
        }

        private static void UpdatePlayerInteraction(Astronaut player)
        {
            if(player == null) return;
            if (player.GameMode == GameMode.Spectator) return;
            if (IsHolding< BlockItem>())
                player.SetInteraction(new InteractionPlaceBlock());
            else if (IsHolding< WeaponItem>())
                player.SetInteraction(new InteractionShoot(SelectedSlot));
            else if (IsHolding< DrillItem>())
            {
                if (player.GameMode == GameMode.Creative)
                    player.SetInteraction(new InteractionDestroyBlockCreative(SelectedSlot));
                else
                    player.SetInteraction(new InteractionDestroyBlockSurvival(SelectedSlot));
            }
            else if (IsHolding< ConsumableItem>())
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
                if(scrollAudio != null)
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

        private static void SelectSlot(short slot)
        {
            if (Storage != null)
            {
                SelectedSlot = Storage.GetSlot(0, slot);


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

                if (SelectedSlot.HasItem)
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

                OnSlotChanged?.Invoke(slot);
            }
        }

        public static void Render()
        {
            PanelRenderer.Render(Storage, SlotSize, SlotTexture, SelectedTexture, SelectedSlotId, _time);

            if(Settings.ShowInterface)
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
                if (Input.IsKey(Keys.LeftShift))
                    slot.MoveItemToConnectedStorage();
                if (Input.IsKey(Keys.LeftAlt))
                    slot.Split();
                if (Input.IsKey(Keys.X))
                    slot.Clear();
                if (Input.IsMouseButton(MouseButton.Left))
                {
                    Vector2 mousePos = Input.Mouse.Position.ToSystemVector2();
                }
            }
        }
    }

 
}
