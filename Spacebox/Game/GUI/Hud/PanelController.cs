using Spacebox.Network.Players;
using Engine;
using Engine.Audio;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Player.Interactions;
using System.Numerics;
using System;

namespace Spacebox.Game.GUI
{
    public sealed class PanelController : IGameUiController
    {
        private readonly BlockLightProbe _heldItemLight = new BlockLightProbe();
        private float SlotSize = 64.0f;
        public bool EnableRenderForCurrentItem { get; set; } = true;
        public bool AllowScroll { get; set; } = true;
        public bool IsItemModelVisible { get; set; } = true;
        public bool AreHandsBlocked { get; private set; }
        public Storage Storage;
        private ItemModel ItemModel;
        private ItemSlot SelectedSlot;
        public Item HeldItem => SelectedSlot != null && SelectedSlot.HasItem ? SelectedSlot.Item : null;
        private AudioSource scrollAudio;
        private AudioSource dropAudio;
        private short _selectedSlotId = 0;
        public short SelectedSlotId
        {
            get { return _selectedSlotId; }
            set { _selectedSlotId = value; }
        }
        public Action<short> OnSlotChanged;

        private static readonly Predicate<Item> RejectAll = _ => false;

        public int ActiveSlots { get; private set; } = HotbarLayout.DefaultSlotsPerRow;
        public int SlotsPerRow { get; private set; } = HotbarLayout.DefaultSlotsPerRow;
        public int Rows { get; private set; } = HotbarLayout.DefaultRows;

        private readonly ItemControlsUI _itemControls;

        public PanelController(ItemControlsUI itemControls)
        {
            _itemControls = itemControls;
        }

        private float TimeToHideItemName = 2f;
        private float _time = 0;
        private bool wasPlayerOnes = false;
        public LocalAstronaut Player;
        private short _lastSelectedSlotId = -1;
        private Item _lastSelectedItem = null;
        private byte _lastSelectedCount = 0;

        public void Initialize(LocalAstronaut player)
        {
            Player = player;
            Storage = Player.Panel;
            scrollAudio = new AudioSource(GameAssets.LoadResource<AudioClip>("Resources/Audio/scroll.ogg"), AudioChannel.UI);
            dropAudio = new AudioSource(GameAssets.LoadResource<AudioClip>("Resources/Audio/dropped.ogg"), AudioChannel.UI);

            ApplyHotbarLayout();
            SetSelectedSlot(0);
            Storage.OnDataWasChanged += OnStorageDataWasChanged;

            _lastSelectedItem = null;
            _lastSelectedSlotId = -1;
            _lastSelectedCount = 0;
        }

        public void Reset()
        {
            if (Storage != null) Storage.OnDataWasChanged -= OnStorageDataWasChanged;
            Player = null;
            Storage = null;
            ItemModel = null;
            SelectedSlot = null;
            IsItemModelVisible = true;
            AreHandsBlocked = false;
        }

        private void OnStorageDataWasChanged(Storage storage)
        {
            SelectSlot(SelectedSlotId);
        }

        public bool IsHolding<T>() where T : Item
        {
            if (SelectedSlot == null) return false;
            if (!SelectedSlot.HasItem) return false;
            return SelectedSlot.Item.Is<T>();
        }

        public void BlockHands()
        {
            if (AreHandsBlocked) return;
            AreHandsBlocked = true;

            if (ItemModel != null) ItemModel.ResetToEnd();
            ItemModel = null;
            _itemControls.IsVisible = false;
            BlockSelector.IsVisible = false;

            if (Player != null)
            {
                Player.SetInteraction(new InteractionDefault());
                Player.ChangeItemInHand(null);
                Player.ItemLight.Enabled = false;
            }

            AstronautNetwork.Local?.SetHeldItem(null);
        }

        public void ResetHandsBlock()
        {
            AreHandsBlocked = false;
        }

        public void UnblockHands()
        {
            if (!AreHandsBlocked) return;
            AreHandsBlocked = false;
            ResetLastSelected();
            SelectSlot(SelectedSlotId);
        }

        public void DrawItemModel()
        {
            if (!IsItemModelVisible || AreHandsBlocked) return;
            if (ItemModel == null) return;
            ItemModel.Render();
        }

        public bool IsHeldItemAnimating => ItemModel is AnimatedItemModel { Animator.IsActive: true };

        public ItemModel GetSlotModel()
        {
            return GameAssets.ItemModels[SelectedSlot.Item.Id];
        }

        public bool TryPlaceItem(out short id)
        {
            id = 0;
            if (SelectedSlot == null) return false;
            if (!SelectedSlot.HasItem) return false;
            if (UIManager.IsOpen("inventory")) return false;
            if (!Spacebox.Network.Containers.ContainerNetwork.CanUseItems) return false;
            if (!(SelectedSlot.Item is BlockItem itemBlock)) return false;
            id = itemBlock.BlockId;
            if (Player == null || Player.GameModeRules.ConsumeItems)
                SelectedSlot.TakeOne();
            return true;
        }

        public void ShowItemModel()
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

        private void HideItemModel()
        {
            if (SelectedSlot == null) return;
            if (!SelectedSlot.HasItem || SelectedSlot.Item is BlockItem)
            {
                if (ItemModel != null)
                    ItemModel.ResetToEnd();

                ItemModel = null;
            }
        }

        private void UpdateInput()
        {
            if (Player == null || Storage == null) return;
            if (Debug.IsVisible) return;
            if (UIManager.IsOpen("inventory")) return;

            if (UIManager.IsUIMode && !UIManager.IsOpen("radar")) return;

            if (!AllowScroll || AreHandsBlocked) return;
            if (Player.CanMove == false) return;

            if (Input.MouseScrollDelta.Y < 0)
            {
                SetSelectedSlot((short)HotbarLayout.Step(SelectedSlotId, 1, ActiveSlots));
            }
            if (Input.MouseScrollDelta.Y > 0)
            {
                SetSelectedSlot((short)HotbarLayout.Step(SelectedSlotId, -1, ActiveSlots));
            }
            if (Input.IsActionDown("drop_item"))
            {
                if (Input.IsKey(Keys.LeftShift))
                {
                    DropItem(SelectedSlot, true);
                }
                else
                {
                    DropItem(SelectedSlot, false);
                }
            }
            if (Input.IsKeyDown(Keys.D0))
            {
                int slot = HotbarLayout.SlotForNumberKey(9, ActiveSlots);
                if (slot >= 0) SetSelectedSlot((short)slot);
            }
            for (int i = 0; i < 9; i++)
            {
                if (Input.IsKeyDown(Keys.D1 + i))
                {
                    int slot = HotbarLayout.SlotForNumberKey(i, ActiveSlots);
                    if (slot >= 0) SetSelectedSlot((short)slot);
                    break;
                }
            }
        }

        private void DropItem(ItemSlot slot, bool dropAll)
        {
            if (slot == null) return;
            if (!slot.HasItem) return;
            if (Player == null) return;
            if (!Player.GameModeRules.UseItems) return;
            if (!Spacebox.Network.Containers.ContainerNetwork.CanDropFrom(slot)) return;

            var dropPosition = Player.Position + Player.Front * 0.5f;

            if (!dropAll)
            {
                World.DropEffectManager.DropItem(dropPosition, Player.Front, 6f, slot.Item, 1, 2, 4);
                slot.DropOne();
            }
            else
            {
                World.DropEffectManager.DropItemSlot(dropPosition, Player.Front, 6f, slot, 2, 4);
                slot.Clear();
            }

            if (dropAudio != null)
            {
                dropAudio.Play();
            }
        }

        public void Update()
        {
            if (_time > 0)
                _time -= Time.Delta;

            if (ItemModel != null && EnableRenderForCurrentItem)
            {
                if (ItemModel.Enabled)
                {
                    if (Player != null && Player.ItemSway != null)
                    {
                        ItemModel.SwayMatrix = Player.ItemSway.GetSwayMatrix();
                    }
                    if (Player != null && ItemModel.Material != null)
                    {
                        _heldItemLight.Update(Player.Position, Time.Delta);
                        ItemModel.Material.BlockLight = _heldItemLight.Value;
                    }
                    else
                    {
                        ItemModel.SwayMatrix = Matrix4.Identity;
                    }
                    ItemModel.Update();
                }
            }

            if (UIManager.IsOpen("inventory")) return;
            UpdateInput();
        }

        private void UpdatePlayerInteraction(LocalAstronaut player)
        {
            if (player == null) return;
            if (!player.GameModeRules.UseItems) return;
            if (IsHolding<BlockItem>())
                player.SetInteraction(new InteractionPlaceBlock());
            else if (IsHolding<WeaponItem>())
                player.SetInteraction(new InteractionShoot(SelectedSlot));
            else if (IsHolding<DrillItem>())
            {
                if (player.GameModeRules.InstantBreak)
                    player.SetInteraction(new InteractionDestroyBlockCreative(SelectedSlot));
                else
                    player.SetInteraction(new InteractionDestroyBlockSurvival(SelectedSlot));
            }
            else if (IsHolding<ConsumableItem>())
                player.SetInteraction(new InteractionConsumeItem(SelectedSlot));
            else if (IsHolding<EquipmentItem>())
                player.SetInteraction(new InteractionEquipItem(SelectedSlot));
            else if (IsHolding<RepairToolItem>())
                player.SetInteraction(new InteractionRepair(SelectedSlot));
            else if (IsHolding<MobSpawnerItem>())
                player.SetInteraction(new InteractionSpawnMob(SelectedSlot));
            else if (IsHolding<EraserToolItem>())
                player.SetInteraction(new InteractionEraser());
            else if (IsHolding<FillToolItem>())
                player.SetInteraction(new InteractionFill());
            else if (IsHolding<CopyToolItem>())
                player.SetInteraction(new InteractionCopyPaste());
            else if (IsHolding<CameraPointItem>())
                player.SetInteraction(new InteractionCameraPoint(ItemModel));
            else
                player.SetInteraction(new InteractionDefault());
        }

        public void ApplyHotbarLayout()
        {
            if (Player == null || Storage == null) return;

            var rules = Player.GameModeRules;
            Rows = HotbarLayout.ClampRows(rules.HotbarRows);
            SlotsPerRow = HotbarLayout.ClampSlotsPerRow(rules.HotbarSlots);
            ActiveSlots = Math.Min(Rows * SlotsPerRow, Storage.SizeY);

            for (int i = 0; i < Storage.SizeY; i++)
            {
                var slot = Storage.GetSlot(0, i);
                bool active = i < ActiveSlots;
                slot.Filter = active ? null : RejectAll;
                slot.IsManualOnly = !active;
                if (!active && slot.HasItem) MoveOutOfHotbar(slot);
            }

            int clamped = HotbarLayout.Clamp(SelectedSlotId, ActiveSlots);
            if (clamped != SelectedSlotId) SetSelectedSlot((short)clamped);
        }

        private void MoveOutOfHotbar(ItemSlot slot)
        {
            var item = slot.Item;
            byte count = slot.Count;
            if (Player.Inventory.TryAddItem(item, count, out byte rest) || rest == 0)
            {
                slot.Clear();
                return;
            }

            slot.Count = rest;
            DropItem(slot, true);
        }

        public void ResetLastSelected()
        {
            _lastSelectedSlotId = -1;
            _lastSelectedCount = 0;
            _lastSelectedItem = null;
        }

        public void SetSelectedSlot(short id)
        {
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

        public void SetFlashlight(LocalAstronaut ast)
        {
            if (ast?.Flashlight != null && ItemModel?.Material != null)
            {
                ItemModel.Material.SetFlashlight(ast.Flashlight);
            }
        }

        private void SelectSlot(short slot)
        {
            if (Storage != null)
            {
                SelectedSlot = Storage.GetSlot(0, slot);
                if (AreHandsBlocked) return;

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

                if (Player != null && SelectedSlot != null)
                {
                    Player.SetItemLight(SelectedSlot);

                    if (SelectedSlot.HasItem)
                    {
                        Player.ChangeItemInHand(SelectedSlot.Item);
                    }
                    else
                    {
                        Player.ChangeItemInHand(null);
                    }
                }

                AstronautNetwork.Local?.SetHeldItem(SelectedSlot);

                ShowItemDescription();

                OnSlotChanged?.Invoke(slot);
            }
        }

        private void ShowItemDescription()
        {
            if (!UIManager.IsOpen("inventory"))
            {
                if (SelectedSlot.HasItem && SelectedSlot.Item != null)
                {
                    _time = TimeToHideItemName;

                    if (SelectedSlot.Item.Description != "")
                    {
                        _itemControls.IsVisible = true;
                        _itemControls.Text = SelectedSlot.Item.Description;
                    }
                    else
                    {
                        _itemControls.IsVisible = false;
                    }
                }
                else
                {
                    _itemControls.IsVisible = false;
                }
            }
        }

        public string ShownItemName => HudVisibility.ShowPlayerHud && SelectedSlot != null && SelectedSlot.HasItem && _time > 0 ? SelectedSlot.Item.Name : null;

        public ItemSlot CurrentSlot() => SelectedSlot;

        public void OnSlotClicked(ItemSlot slot)
        {
            if (Input.IsMouseButton(MouseButton.Left))
            {
            }
            if (slot.HasItem)
            {
                if (Input.IsAction("storage_item_quick_transfer"))
                {
                    if (Player == null || !Player.Equipment.TryEquip(slot))
                        slot.TryMoveItemToConnectedStorage(out var _);
                }
                if (Input.IsKey(Keys.LeftAlt))
                    slot.Split();
                if (Input.IsAction("storage_item_delete"))
                    slot.Clear();
                if (Input.IsMouseButton(MouseButton.Left))
                {
                    var mousePos = Input.Mouse.Position.ToSystemVector2();
                }
            }
        }
    }
}