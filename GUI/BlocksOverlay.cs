using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Game;
using System;
using System.Collections.Generic;

namespace Spacebox.GUI
{
    public class BlocksOverlay
    {
        private static short selectedBlockId = 0;
        private static Dictionary<short, Texture2D> _blockIcons = new Dictionary<short, Texture2D>();

        private static Astronaut astronaut;
        private static bool isSubscribed = false;

        public static void OnGUI(Node3D player)
        {
            // Проверяем, является ли player объектом Astronaut и подписываемся на событие
            if (astronaut == null)
            {
                astronaut = player as Astronaut;
                if (astronaut != null && !isSubscribed)
                {
                    astronaut.OnCurrentBlockChanged += Astronaut_OnCurrentBlockChanged;
                    isSubscribed = true;

                    // Инициализируем selectedBlockId текущим блоком астронавта
                    selectedBlockId = astronaut.CurrentBlockId;
                }
            }

            // Проверяем, зажат ли ALT
            bool isAltPressed = Input.IsKey(Keys.LeftAlt) || Input.IsKey(Keys.RightAlt);

            // Устанавливаем состояние курсора
            if (isAltPressed)
            {
                Input.SetCursorState(CursorState.Normal);

                astronaut.CanMove = false;
            }
            else
            {
                Input.SetCursorState(CursorState.Grabbed);
                astronaut.CanMove = true;
            }

            var io = ImGui.GetIO();
            int screenWidth = (int)io.DisplaySize.X;
            int screenHeight = (int)Window.Instance.ClientSize.Y;

            float windowWidth = screenWidth * 0.15f; // 15% ширины экрана
            float windowHeight = screenHeight - 80;
            float windowX = screenWidth - windowWidth - 20;
            float windowY = 40;

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0, 0, 0, 0.8f));
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(windowWidth, windowHeight), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(windowX, windowY), ImGuiCond.Always);
            ImGui.Begin("Block List", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);

            ImGui.Spacing();
            ImGui.Text(" ");
            ImGui.Text("Blocks:");
            ImGui.Separator();
            ImGui.Text(" ");

            // Начинаем область со скроллингом для длинного списка
            ImGui.BeginChild("BlockListScrollingRegion",
                new System.Numerics.Vector2(0, -ImGui.GetFrameHeightWithSpacing()), ImGuiChildFlags.AutoResizeY);
            ImGui.Text(" ");

            foreach (var kvp in GameBlocks.Block)
            {
                short id = kvp.Key;
                BlockData blockData = kvp.Value;

                bool isSelected = (id == selectedBlockId);

               
                if (!_blockIcons.TryGetValue(id, out Texture2D blockIcon))
                {
                    // Получаем координаты текстуры блока в атласе
                    int atlasX = (int)blockData.TextureCoords.X;
                    int atlasY = (int)blockData.TextureCoords.Y;

                    // Получаем текстуру блока из атласа
                    blockIcon = IsometricIcon.CreateIsometricIcon(UVAtlas.GetBlockTexture(GameBlocks.BlocksTexture, atlasX, atlasY));
                    _blockIcons[id] = blockIcon;
                }

                // Получаем идентификатор текстуры для ImGui
                IntPtr textureId = (IntPtr)blockIcon.Handle;

                // Уникальный ID для элементов
                ImGui.PushID(id);

                // Начинаем линию для иконки и текста
                ImGui.BeginGroup();

                // Отображаем иконку блока
                ImGui.Image(textureId, new System.Numerics.Vector2(32, 32)); // Размер иконки можно настроить

                ImGui.SameLine();

                // Опции для ImGui.Selectable
                ImGuiSelectableFlags selectableFlags = ImGuiSelectableFlags.None;
                if (!isAltPressed)
                {
                    selectableFlags |= ImGuiSelectableFlags.Disabled;
                }

                // Используем ImGui.Selectable для отображения текста и обработки выбора
                if (ImGui.Selectable($"ID: {id}, {blockData.Name}", isSelected, selectableFlags))
                {
                    selectedBlockId = id;
                    if (astronaut != null)
                    {
                        astronaut.CurrentBlockId = id;
                    }
                }

                ImGui.EndGroup();
                ImGui.PopID();

                // Добавляем всплывающую подсказку при наведении
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Image(textureId, new System.Numerics.Vector2(64, 64)); // Больший размер для тултипа
                    ImGui.Text($"ID: {id}");
                    ImGui.Text($"Name: {blockData.Name}");
                    ImGui.Text($"Transparent: {blockData.IsTransparent}");
                    ImGui.EndTooltip();
                }
            }

            ImGui.EndChild();

            ImGui.End();
            ImGui.PopStyleColor();
        }

        // Обработчик изменения текущего блока в Astronaut
        private static void Astronaut_OnCurrentBlockChanged(short newBlockId)
        {
            selectedBlockId = newBlockId;
        }

        // Метод для освобождения ресурсов
        public static void Dispose()
        {
            // Отписываемся от события
            if (astronaut != null && isSubscribed)
            {
                astronaut.OnCurrentBlockChanged -= Astronaut_OnCurrentBlockChanged;
                isSubscribed = false;
                astronaut = null;
            }

            foreach (var icon in _blockIcons.Values)
            {
                icon.Dispose();
            }
            _blockIcons.Clear();
        }
    }
}
