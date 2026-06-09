using Engine;
using Engine.Audio;
using ImGuiNET;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using System;
using System.Numerics;
using System.Text;

namespace Spacebox.Game.GUI
{
    public static class StorageUI
    {
        private static float SlotSize = 64.0f;
        private static nint PencilTexture = nint.Zero;
        private static Storage? Storage;
        private static StorageBlock? StorageBlock;
        private static LocalAstronaut? Astronaut;
        private static bool _wasVisible = false;

        private static AudioSource openSound;
        private static AudioSource closeSound;
        private static AudioSource splitAudio;

        private static bool editingName = false;
        private static byte[] buffer = new byte[32];

        public static void Initialize(nint textureId)
        {
            var itemTexture = Resources.Load<Texture2D>("Resources/Textures/UI/trash.png");
            itemTexture.FilterMode = FilterMode.Nearest;
            itemTexture.FlipY();

            var pencil = Resources.Load<Texture2D>("Resources/Textures/UI/pencil.png");
            pencil.FilterMode = FilterMode.Nearest;
            pencil.FlipY();
            PencilTexture = pencil.Handle;

            InventoryUIHelper.SetDefaultIcon(textureId, nint.Zero);

            splitAudio = new AudioSource(Resources.Load<AudioClip>("splitStack"));
            openSound = new AudioSource(Resources.Get<AudioClip>("openStorage"));
            closeSound = new AudioSource(Resources.Get<AudioClip>("closeStorage"));
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
            editingName = false;
            buffer = new byte[32];
        }

        public static void OnGUI()
        {
            bool isVisible = UIManager.IsOpen("storage");

            if (isVisible != _wasVisible)
            {
                if (isVisible)
                {
                    openSound?.Play();
                }
                else
                {
                    closeSound?.Play();
                    CloseStorage();
                }
                _wasVisible = isVisible;
            }

            if (!isVisible || Storage == null) return;

            var io = ImGui.GetIO();
            var displaySize = io.DisplaySize;
            var style = ImGui.GetStyle();

            SlotSize = InventoryUIHelper.SlotSize;

            float windowWidth = Storage.SizeX * SlotSize;
            float windowHeight = Storage.SizeY * SlotSize;

            Vector2 windowPos = new Vector2(
                (displaySize.X - windowWidth) / 2f,
                (displaySize.Y - windowHeight) / 4f);

            var padding = SlotSize * 0.1f;
            var paddingV = new Vector2(padding, padding);

            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight + padding * 4) + paddingV + paddingV);
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoDecoration |
                                           ImGuiWindowFlags.NoScrollbar |
                                           ImGuiWindowFlags.NoScrollWithMouse;

            ImGui.Begin("Storage", windowFlags);

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight + padding * 4) + paddingV + paddingV, displaySize.Y);

            if (Storage.SizeX >= 2)
            {
                ImGui.SetCursorPos(paddingV);

                if (!editingName)
                {
                    var stb = StorageBlock;
                    ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1f), stb?.Name ?? Storage.Name);
                    ImGui.SameLine();
                    if (stb != null && ImGui.ImageButton("##edit", PencilTexture, paddingV * 4))
                    {
                        StartNameEdit();
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Rename");
                        ImGui.EndTooltip();
                    }
                }
                else
                {
                    float buttonWidth = ImGui.CalcTextSize("Save").X + style.FramePadding.X * 2;
                    ImGui.SetNextItemWidth(windowWidth - buttonWidth - windowWidth / 2f);
                    bool enterPressed = ImGui.InputText("##storageName", buffer, (uint)buffer.Length, ImGuiInputTextFlags.EnterReturnsTrue);
                    ImGui.SameLine();
                    if (ImGui.SmallButton("Save") || enterPressed)
                    {
                        SaveName();
                    }
                }
            }

            if (Storage.SizeX >= 3)
            {
                ImGui.SameLine();
                InventoryUIHelper.SortStorageButtons(windowWidth, padding, Storage);
            }

            ImGui.SetCursorPos(paddingV + new Vector2(0, padding * 4));
            InventoryUIHelper.RenderStorage(Storage, OnSlotClicked, Storage.SizeX);

            ImGui.End();
        }

        private static void StartNameEdit()
        {
            if (StorageBlock == null) return;
            editingName = true;
            Array.Clear(buffer, 0, buffer.Length);
            var bytes = Encoding.UTF8.GetBytes(StorageBlock.Name);
            Array.Copy(bytes, buffer, Math.Min(bytes.Length, buffer.Length - 1));
            ImGui.SetKeyboardFocusHere();
        }

        private static void SaveName()
        {
            if (StorageBlock == null) return;

            int len = Array.IndexOf(buffer, (byte)0);
            if (len < 0) len = buffer.Length;

            string newName = Encoding.UTF8.GetString(buffer, 0, len).Trim();
            if (!string.IsNullOrWhiteSpace(newName))
            {
                StorageBlock.Name = newName;
            }

            editingName = false;
        }

        private static void TryMoveToPlayerPanel(ItemSlot slot, byte rest)
        {
            if (Astronaut?.Panel == null) return;

            if (Astronaut.Panel.TryAddItem(slot.Item, rest))
            {
                slot.Count = (byte)(slot.Count - rest);
            }
        }

        private static void OnSlotClicked(ItemSlot slot)
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

            PencilTexture = nint.Zero;
            Storage = null;
            StorageBlock = null;
            Astronaut = null;
        }
    }
}