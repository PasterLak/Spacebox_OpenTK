using Engine;
using Engine.Audio;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using System;

namespace Spacebox.Game.GUI
{
    public sealed class StorageUI : IGameUiController
    {
        public Storage? Storage { get; private set; }
        public StorageBlock? StorageBlock { get; private set; }
        private LocalAstronaut? Astronaut;
        private bool _wasVisible = false;

        private AudioSource splitAudio;


        public void Initialize(LocalAstronaut player)
        {
            splitAudio = new AudioSource(Resources.Load<AudioClip>("splitStack"), AudioChannel.UI);
        }

        public void OpenStorage(StorageBlock storageBlock, LocalAstronaut astronaut)
        {
            StorageBlock = storageBlock;
            OpenStorage(storageBlock.Storage, astronaut);
        }

        public void OpenStorage(Storage storage, LocalAstronaut astronaut)
        {
            Storage = storage;
            Astronaut = astronaut;
            Storage.ConnectStorage(astronaut.Inventory);
            astronaut.Inventory.ConnectStorage(Storage);
            astronaut.Panel.ConnectStorage(Storage);

            if (astronaut.GameMode != GameMode.Survival)
                UIManager.Open("storage", "inventory", "creative");
            else
                UIManager.Open("storage", "inventory");
        }

        private void CloseStorage()
        {
            Storage?.DisconnectStorage();
            Storage = null;
            StorageBlock = null;

            if (Astronaut is not null)
            {
                Astronaut.Inventory.ConnectStorage(Astronaut.Panel);
                Astronaut.Panel.ConnectStorage(Astronaut.Inventory, true);
                Astronaut = null;
            }
        }

        public void OnGUI()
        {
            bool isVisible = UIManager.IsOpen("storage");
            if (isVisible == _wasVisible) return;

            if (!isVisible) CloseStorage();
            _wasVisible = isVisible;
        }

        public void Rename(string name)
        {
            if (StorageBlock == null) return;
            string newName = (name ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(newName)) Spacebox.Network.Containers.StorageNameNetwork.Rename(StorageBlock, newName);
        }

        private void TryMoveToPlayerPanel(ItemSlot slot, byte rest)
        {
            if (Astronaut?.Panel == null) return;

            Astronaut.Panel.TryAddItem(slot.Item, rest, out var notAdded);
            if (notAdded < rest)
            {
                slot.Count = (byte)(slot.Count - (rest - notAdded));
            }
        }

        public void OnSlotClicked(ItemSlot slot)
        {
            if (!slot.HasItem) return;

            if (Input.IsAction("storage_item_quick_transfer"))
            {
                if (slot.TryMoveItemToConnectedStorage(out var rest))
                {
                }
                else
                {
                    TryMoveToPlayerPanel(slot, rest);
                }
            }
            else if (Input.IsAction("storage_item_delete"))
            {
                slot.Clear();
            }
        }

        public void Reset()
        {
            splitAudio?.Dispose();

            Storage = null;
            StorageBlock = null;
            Astronaut = null;
            _wasVisible = false;
        }
    }
}