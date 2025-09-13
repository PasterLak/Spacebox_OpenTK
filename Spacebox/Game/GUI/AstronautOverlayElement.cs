using ImGuiNET;
using Engine;
using Engine.GUI;
using Spacebox.Game.Player;

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

                    string formatted = ast.InertiaController.Velocity.Length.ToString("0.0");
                    ImGui.Text($"Speed: {formatted}");
               
                ImGui.Text($"Gamemode: {ast.GameMode}");
                var mood = ast.Mood;
                var trend = mood.CalculateMoodTrend();
                var time = mood.CalculateTimeToTarget(trend > 0 ? 100 : 0).ToString("F1");
                ImGui.Text($"Mood: {mood.MoodData.Value} Trend: {trend.ToString("F1")} ({time}s.) ");

                if (ast.CurrentInteraction != null)
                ImGui.Text($"Interaction: {ast.CurrentInteraction.GetType().Name}");
            }
        }

       
    }
}
