using ImGuiNET;
using Engine;
using Engine.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Generation;

namespace Spacebox.Game.GUI
{
    public class AstronautOverlayElement : OverlayElement
    {

        public override void OnGUIText()
        {
            if (Camera.Main != null)
            {
                var cam = Camera.Main;
                Astronaut ast = cam as Astronaut;

                if (ast == null) return;

                ImGui.SeparatorText("[PLAYER]");
                string formatted = ast.InertiaController.Velocity.Length.ToString("0.0");
                    ImGui.Text($"Speed: {formatted} Gamemode: {ast.GameMode}");

                if (ast.CurrentInteraction != null)
                    ImGui.Text($"Interaction: {ast.CurrentInteraction.GetType().Name}");

                if (World.CurrentSector != null && World.CurrentSector.BiomesMap != null)
                {
                    var pos = World.CurrentSector.WorldToLocalPosition(ast.PositionWorld);

                    var biom = World.CurrentSector.BiomesMap.GetFromSectorLocalCoord(pos);

                    ImGui.Text("Biome:"); ImGui.SameLine();
                    ImGui.TextColored(biom.DebugColor.ToSystemVector4(), $"{biom.Name}");

                }
                    


                var size = ImGui.CalcTextSize("M");
                var mood = ast.Mood;
                var trend = mood.CalculateMoodTrend();
                var time = mood.CalculateTimeToTarget(trend > 0 ? 100 : 0).ToString("F1");
                ImGui.Text("Mood: "); ImGui.SameLine();
                ImGui.ProgressBar(mood.MoodData.Value / 100f, new System.Numerics.Vector2(size.Y * 5, size.Y), $"{mood.MoodData.Value}/{100}");
                ImGui.SameLine();
                ImGui.Text($"Trend: {trend.ToString("F1")} ({time}s.) ");

                
            }
        }

       
    }
}
