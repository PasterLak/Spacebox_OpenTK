using System.Numerics;
using Engine;
using Engine.Audio;

using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Engine.InputPro;

namespace Spacebox.Game.GUI
{
    public sealed class InventoryUI : IGameUiController
    {
        private readonly ChatController _chat;
        private readonly ItemControlsUI _itemControls;

        public LocalAstronaut Player;
        private bool _wasOpen;

        public InventoryUI(ChatController chat, ItemControlsUI itemControls)
        {
            _chat = chat;
            _itemControls = itemControls;
        }

        public void OnGUI()
        {
            HandleInput();
            bool open = UIManager.IsOpen("inventory");
            if (_wasOpen && !open) _itemControls.IsVisible = false;
            _wasOpen = open;
        }

        public AudioSource splitAudio;

        public void Initialize(LocalAstronaut player)
        {
            Player = player;
            splitAudio = new AudioSource(Resources.Load<AudioClip>("splitStack"), AudioChannel.UI);
        }

        public void Reset()
        {
            splitAudio?.Dispose();
            splitAudio = null;
            Player = null;
            _wasOpen = false;
        }

        private void HandleInput()
        {
            if (Input.IsActionDown("inventory") && !Debug.IsVisible && !Spacebox.Game.Ships.ShipPilot.IsPiloting)
            {
                if (Player.IsAlive == false) return;
                if (_chat.FocusInput) return;
                if (Spacebox.Game.GUI.Canvas.GameCanvas.WantsKeyboard) return;

                if (!UIManager.IsUIMode)
                {
                    if (Player.GameModeRules.CreativeInventory)
                    {
                        UIManager.Open("inventory", "creative");
                    }
                    else
                    {
                        UIManager.Open("inventory");
                    }
                }
                else
                {

                    UIManager.CloseAll();
                }

                if (UIManager.IsOpen("inventory"))
                {
                    _itemControls.IsVisible = true;

                    var shift = InputManager.Instance.GetAction("storage_item_quick_transfer");
                    var delete = InputManager.Instance.GetAction("storage_item_delete");

                    var text = shift.Bindings[0].GetDisplayName() + " - " + shift.Description;
                    text += "\n" + delete.Bindings[0].GetDisplayName() + " - " + delete.Description;

                    _itemControls.Text = text;
                }
                else
                {
                    _itemControls.IsVisible = false;
                }
            }
        }

        public void OnSlotClicked(ItemSlot slot)
        {
            if (slot.HasItem)
            {
                if (Input.IsAction("storage_item_quick_transfer"))
                {
                    if (Player == null || !Player.Equipment.TryEquip(slot))
                        slot.TryMoveItemToConnectedStorage(out var _);
                }
                if (Input.IsAction("storage_item_delete"))
                {
                    slot.Clear();
                }
            }
        }
    }
}
