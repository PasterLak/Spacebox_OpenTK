using Engine;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;
using System.Numerics;

namespace Spacebox.Game.GUI
{
    public class GeneratorUI
    {
        private static GeneratorBlock generatorBlock;
        public static bool IsVisible { get; set; } = false;

        public static void Initialize()
        {
            var inventory = ToggleManager.Register("generator");
            inventory.IsUI = true;
            inventory.OnStateChanged += s => IsVisible = s;
        }

        public static void Open(GeneratorBlock block, Astronaut astronaut)
        {
            generatorBlock = block;

            if (ToggleManager.IsActiveAndExists("pause")) return;
            var v = IsVisible;
            bool count = ToggleManager.IsActiveAndExists("radar");
            

            if (!count)
                IsVisible = !v;

            if (IsVisible)
            {
                ToggleManager.SetState("mouse", true);
                ToggleManager.SetState("player", false);
               // ToggleManager.SetState("panel", false);

            }
            else
            {
                ToggleManager.SetState("mouse", false);
                ToggleManager.SetState("player", true);
                ToggleManager.DisableAllWindows();
            }
        }

        public static void Close()
        {
            generatorBlock = null;
            IsVisible = false;
            ToggleManager.SetState("mouse", false);
            ToggleManager.SetState("player", true);
        }

        public static void OnGUI()
        {
            if (!IsVisible || generatorBlock == null) return;

            if (Input.IsKeyDown(Keys.Tab) || Input.IsKeyDown(Keys.Escape))
            {
                Close();
                return;
            }

            var io = ImGui.GetIO();
            var displaySize = io.DisplaySize;

            float windowWidth = displaySize.X * 0.15f;
            float windowHeight = displaySize.Y * 0.2f;

            Vector2 windowPos = new Vector2(
                (displaySize.X - windowWidth) * 0.5f,
                (displaySize.Y - windowHeight) * 0.5f);

            var padding = windowHeight/10f;
            var paddingV = new Vector2(padding, padding);

            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));

            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoDecoration |
                                           ImGuiWindowFlags.NoScrollbar;

            if (ImGui.Begin("Generator", windowFlags))
            {
                GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y);

                ImGui.SetCursorPos(paddingV);
                ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1f), "Generator Block");

                ImGui.SetCursorPos(paddingV + new Vector2(0, padding * 1.5f));

                if (ImGui.BeginChild("Content", new Vector2(windowWidth - padding * 2, windowHeight - padding * 5), ImGuiChildFlags.None))
                {
                    ImGui.Separator();
                    ImGui.Text($"Generation Rate: {generatorBlock.GenerationRate} EU");

                   

                    var progressBarWidth = windowWidth - padding * 4;
                    var powerRatio = generatorBlock.MaxPower > 0 ? (float)generatorBlock.CurrentPower / generatorBlock.MaxPower : 0f;

                   // ImGui.Text("Power Level:");
                   // ImGui.ProgressBar(powerRatio, new Vector2(progressBarWidth, 20f),
                   //     $"{generatorBlock.CurrentPower}/{generatorBlock.MaxPower}");
                }
                ImGui.EndChild();
            }
            ImGui.End();
        }
    }
}