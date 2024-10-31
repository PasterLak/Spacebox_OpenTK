using ImGuiNET;
using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Entities;
using Spacebox.Game; // Не забудьте добавить этот using

namespace Spacebox.GUI
{
    public class BlocksOverlay
    {
        // Дополнительно: переменная для хранения выбранного блока
        private static short selectedBlockId = -1;

        public static void OnGUI(Node3D player)
        {
           

            var io = ImGui.GetIO();
            int screenWidth = (int)io.DisplaySize.X;
            int screenHeight = (int)io.DisplaySize.Y;

         
            float windowWidth = 800; // Ширина окна списка блоков
            float windowHeight = screenHeight - 80; // Высота окна, можно настроить
            float windowX = screenWidth - windowWidth - 20; // Отступ от правого края
            float windowY = 40; // Отступ от верхнего края

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0, 0, 0, 0.8f));
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(windowWidth, windowHeight), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(windowX, windowY), ImGuiCond.Always);
            ImGui.Begin("Block List", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);

            ImGui.Spacing();
           
            ImGui.Text($" ");
            ImGui.Text("Blocks:");
            ImGui.Separator();

            ImGui.BeginChild("BlockListScrollingRegion", new System.Numerics.Vector2(0, -ImGui.GetFrameHeightWithSpacing()),
    ImGuiChildFlags.AutoResizeY);


            foreach (var kvp in GameBlocks.Block)
            {
                short id = kvp.Key;
                BlockData blockData = kvp.Value;

                bool isSelected = (id == selectedBlockId);
                if (ImGui.Selectable($"ID: {id}, Name: {blockData.Name}", isSelected))
                {
                    selectedBlockId = id;
                    // Дополнительно: если хотите установить выбранный блок для игрока
                    // if (player is Astronaut astronaut)
                    // {
                    //     astronaut.CurrentBlockId = id;
                    // }
                }
            }

            ImGui.EndChild();

            ImGui.End();
            ImGui.PopStyleColor();
        }
    }
}
