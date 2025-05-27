using System.Numerics;
using System.Text;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Engine;
using Engine.Audio;

using Spacebox.Game.Player;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Generation.Blocks;
using Spacebox.GUI;

namespace Spacebox.Game.GUI
{
    public static class StorageUI
    {
        private static float SlotSize = 64.0f;
        private static nint SlotTexture = nint.Zero;
        private static nint ItemTexture = nint.Zero;
        private static nint PencilTexture = nint.Zero;
        private static Storage? Storage;
        private static StorageBlock? StorageBlock;
        private static Astronaut? Astronaut;
        public static bool IsVisible { get; set; } = false;

        private static AudioSource openSound;
        private static AudioSource closeSound;
        private static AudioSource splitAudio;

        private static bool editingName = false;
        private static byte[] buffer = new byte[32];

        public static void Initialize(nint textureId)
        {
            SlotTexture = textureId;
            var itemTexture = Resources.Load<Texture2D>("Resources/Textures/UI/trash.png");
            itemTexture.FilterMode = FilterMode.Nearest;
            itemTexture.FlipY();
            ItemTexture = itemTexture.Handle;

            var pencil = Resources.Load<Texture2D>("Resources/Textures/UI/pencil.png");
            pencil.FilterMode = FilterMode.Nearest;
            pencil.FlipY();
            PencilTexture = pencil.Handle;

            InventoryUIHelper.SetDefaultIcon(textureId, nint.Zero);

            var inventory = ToggleManager.Register("storage");
            inventory.IsUI = true;
            inventory.OnStateChanged += s => IsVisible = s;

            splitAudio = new AudioSource(SoundManager.GetClip("splitStack"));
            openSound = new AudioSource(Resources.Get<AudioClip>("openStorage"));
            closeSound = new AudioSource(Resources.Get<AudioClip>("closeStorage"));
        }

        public static void OpenStorage(StorageBlock storageBlock, Astronaut astronaut)
        {
            StorageBlock = storageBlock;
            Storage = storageBlock.Storage;
            Astronaut = astronaut;
            Storage.ConnectStorage(astronaut.Inventory);
            astronaut.Inventory.ConnectStorage(Storage);

            openSound?.Play();
            if (ToggleManager.IsActiveAndExists("pause")) return;
            var v = IsVisible;

            bool count = ToggleManager.IsActiveAndExists("radar");

            ToggleManager.DisableAllWindows();

            if (!count)
                IsVisible = !v;

            if (IsVisible)
            {
                ToggleManager.SetState("mouse", true);
                ToggleManager.SetState("player", false);

                if (astronaut.GameMode != GameMode.Survival)
                    ToggleManager.SetState("creative", true);

                ToggleManager.SetState("inventory", true);
            }
            else
            {
                ToggleManager.SetState("mouse", false);
                ToggleManager.SetState("player", true);
                ToggleManager.SetState("inventory", false);
            }

            ToggleManager.SetState("panel", !IsVisible);
        }

        public static void CloseStorage()
        {
            Storage?.DisconnectStorage();
            Storage = null;
            StorageBlock = null;
            IsVisible = false;
            closeSound?.Play();
            if (Astronaut is not null)
            {
                Astronaut.Inventory.ConnectStorage(Astronaut.Panel);
                Astronaut = null;
            }
            editingName = false;
            buffer = new byte[32];
        }

        public static void OnGUI()
        {
            if (!IsVisible || Storage == null) return;

            if (Input.IsKeyDown(Keys.Tab) || Input.IsKeyDown(Keys.Escape))
            {
                CloseStorage();
                return;
            }

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

            ImGui.SetCursorPos(paddingV);
            if (!editingName)
            {
                ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1f), StorageBlock?.Name ?? "Storage");
                ImGui.SameLine();
                if (ImGui.ImageButton("##edit", PencilTexture, paddingV * 4))
                {
                    StartNameEdit();
                }
            }
            else
            {
                float buttonWidth = ImGui.CalcTextSize("Save").X + style.FramePadding.X * 2;
                ImGui.SetNextItemWidth(windowWidth - buttonWidth - padding * 3);
                bool enterPressed = ImGui.InputText("##storageName", buffer, (uint)buffer.Length, ImGuiInputTextFlags.EnterReturnsTrue);
                ImGui.SameLine();
                if (ImGui.SmallButton("Save") || enterPressed)
                {
                    SaveName();
                }
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

        private static void OnSlotClicked(ItemSlot slot)
        {
            if (!slot.HasItem) return;

            if (Input.IsKey(Keys.LeftShift))
            {
                slot.MoveItemToConnectedStorage();
            }
            else if (Input.IsKey(Keys.X))
            {
                slot.Clear();
            }
        }
    }
}
