using Engine;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.GUI;
using System.Drawing;
using System.Numerics;

namespace Spacebox.Game.GUI
{
    public class GeneratorUI
    {
        private static GeneratorBlock generatorBlock;
        public static bool IsVisible { get; set; } = false;

        static int power;
        static int consum;
        static string blockName = "Generator";
        public static void Initialize()
        {
            var inventory = ToggleManager.Register("generator");
            inventory.IsUI = true;
            inventory.OnStateChanged += s => IsVisible = s;
        }

        public static void Open(GeneratorBlock block, Astronaut astronaut, ref HitInfo hit)
        {
            generatorBlock = block;

            var pos = hit.blockPositionEntity;
             var id = hit.chunk.SpaceEntity.ElectricManager.GetNetworkId((pos.X, pos.Y, pos.Z));
            (power, consum) = hit.chunk.SpaceEntity.ElectricManager.GetNetworkPowerFlow(id);



            blockName = GameAssets.GetBlockDataById(block.Id).Name;

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

            if (Input.IsActionDown("inventory") || Input.IsKeyDown(Keys.Escape))
            {
                Close();
                return;
            }

            var io = ImGui.GetIO();
            var displaySize = io.DisplaySize;

            float windowWidth = displaySize.X * 0.2f;
            float windowHeight = displaySize.Y * 0.25f;

            Vector2 windowPos = new Vector2(
                (displaySize.X - windowWidth) * 0.5f,
                (displaySize.Y - windowHeight) * 0.5f);

            var padding = windowHeight/15f;
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
                ImGui.TextColored(new Vector4(1, 1, 0, 1), blockName);

                ImGui.SetCursorPos(paddingV + new Vector2(0, padding * 1.5f));

                if (ImGui.BeginChild("Content", new Vector2(windowWidth - padding * 2, windowHeight - padding * 5), ImGuiChildFlags.None))
                {
                    ImGui.Separator();
                    ImGui.Text($"Generating: +{generatorBlock.GenerationRate} EU");
                    ImGui.Text($"");
                    ImGui.TextColored(new Vector4(1,1,0,1), "Network");

                    ImGui.Separator();

                    ImGui.Text($"Power: {power} EU");
                    ImGui.Text($"Consumption: {consum} EU");        

                    var progressBarWidth = windowWidth * 0.9f;
                    var powerRatio = generatorBlock.MaxPower > 0 ? (float)generatorBlock.CurrentPower / generatorBlock.MaxPower : 0f;

                    Vector4 progressColor = ConsumptionToColor();
                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, progressColor);
                    var v = consum / (float)power;
                    if (v > 0.99f) v = 0.99f;
                    ImGui.ProgressBar(1 - v, new Vector2(progressBarWidth, windowHeight / 40f), "");
                    ImGui.PopStyleColor();
                   
                    //ImGui.SameLine(); consum = 45;
                    //ImGui.TextColored(ConsumptionToColor(), consum.ToString()); ImGui.SameLine();
                   // ImGui.Text($"EU");

                  

               

                }
                ImGui.EndChild();
            }
            ImGui.End();
        }

        private static Vector4 ConsumptionToColor()
        {
            if (power == 0) return new Vector4(1, 1, 1, 1); 

            var consumptionPercentage = (consum / (float)power) * 100;
            var remainingPercentage = 100 - consumptionPercentage;

            if (remainingPercentage < 33)
            {
                return new Vector4(1, 0, 0, 1); 
            }
            else if (remainingPercentage < 50)
            {
                return new Vector4(1, 0.5f, 0, 1); 
            }
            else if (remainingPercentage < 70)
            {
                return new Vector4(1, 1, 0, 1);
            }
            else
            {
                return new Vector4(0, 1, 0, 1); 
            }
        }
    }
}