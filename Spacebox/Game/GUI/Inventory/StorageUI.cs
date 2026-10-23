using Engine;
using Engine.Audio;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using System;

namespace Spacebox.Game.GUI
{
    public static class StorageUI
    {
        public static Storage? Storage { get; private set; }
        public static StorageBlock? StorageBlock { get; private set; }
        private static LocalAstronaut? Astronaut;
        private static bool _wasVisible = false;

        private static AudioSource openSound;
        private static AudioSource closeSound;
        private static AudioSource splitAudio;


        public static void Initialize()
        {
            splitAudio = new AudioSource(Resources.Load<AudioClip>("splitStack"), AudioChannel.UI);
            openSound = new AudioSource(Resources.Get<AudioClip>("openStorage"), AudioChannel.UI);
            closeSound = new AudioSource(Resources.Get<AudioClip>("closeStorage"), AudioChannel.UI);
        }

        public static void OpenStorage(StorageBlock storageBlock, LocalAstronaut astronaut)
        {
            StorageBlock = storageBlock;
            OpenStorage(storageBlock.Storage, astronaut);
        }

        public static void OpenStorage(Storage storage, LocalAstronaut astronaut)
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

        private static void CloseStorage()
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

        public static void Update()
        {
            bool isVisible = UIManager.IsOpen("storage");
            if (isVisible == _wasVisible) return;

            if (isVisible) openSound?.Play();
            else
            {
                closeSound?.Play();
                CloseStorage();
            }
            _wasVisible = isVisible;
        }

        public static void Rename(string name)
        {
            if (StorageBlock == null) return;
            string newName = (name ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(newName)) Spacebox.Network.Containers.StorageNameNetwork.Rename(StorageBlock, newName);
        }

        private static void TryMoveToPlayerPanel(ItemSlot slot, byte rest)
        {
            if (Astronaut?.Panel == null) return;

            Astronaut.Panel.TryAddItem(slot.Item, rest, out var notAdded);
            if (notAdded < rest)
            {
                slot.Count = (byte)(slot.Count - (rest - notAdded));
            }
        }

        public static void OnSlotClicked(ItemSlot slot)
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

        public static void Dispose()
        {
            closeSound?.Dispose();
            openSound?.Dispose();
            splitAudio?.Dispose();

            Storage = null;
            StorageBlock = null;
            Astronaut = null;
        }
    }
}