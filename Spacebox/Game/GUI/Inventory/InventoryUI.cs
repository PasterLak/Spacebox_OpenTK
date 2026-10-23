using System.Numerics;
using ImGuiNET;
using Engine;
using Engine.Audio;

using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Engine.InputPro;

namespace Spacebox.Game.GUI
{
    public static class InventoryUI
    {
        public static LocalAstronaut Player;
        public static void Update()
        {
            HandleInput();
        }

        public static AudioSource splitAudio;

        public static void Initialize()
        {
            splitAudio = new AudioSource(Resources.Load<AudioClip>("splitStack"), AudioChannel.UI);
        }

        private static void HandleInput()
        {
            if (Input.IsActionDown("inventory") && !Debug.IsVisible && !Spacebox.Game.Ships.ShipPilot.IsPiloting)
            {
                if (Player.IsAlive == false) return;
                if (Chat.FocusInput) return; 
                if (ImGui.GetIO().WantCaptureKeyboard || Spacebox.Game.GUI.Canvas.GameCanvas.WantsKeyboard) return;

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
                    ItemControlsUI.IsVisible = true;

                    var shift = InputManager.Instance.GetAction("storage_item_quick_transfer");
                    var delete = InputManager.Instance.GetAction("storage_item_delete");

                    var text = shift.Bindings[0].GetDisplayName() + " - " + shift.Description;
                    text += "\n" + delete.Bindings[0].GetDisplayName() + " - " + delete.Description;

                    ItemControlsUI.Text = text;
                }
                else
                {
                    ItemControlsUI.IsVisible = false;
                }
            }
        }

        public static void OnSlotClicked(ItemSlot slot)
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