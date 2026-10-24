using Engine;
using Engine.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Generation;

namespace Spacebox.Game.GUI
{
    public class AstronautOverlayElement : OverlayElement
    {

        public override void Write(OverlayText text)
        {
            if (Camera.Main != null)
            {
                var cam = Camera.Main;
                LocalAstronaut ast = cam as LocalAstronaut;

                if (ast == null) return;

                text.Section("[PLAYER]");
                string formatted = ast.InertiaController.Velocity.Length.ToString("0.0");
                    text.Line($"Speed: {formatted} Gamemode: {ast.GameMode}");

                if (ast.CurrentInteraction != null)
                    text.Line($"Interaction: {ast.CurrentInteraction.GetType().Name}");

                bool inAir = World.Instance != null && World.Instance.IsBreathable(ast.Position);
                text.Line($"In air: {(inAir ? "yes" : "no")}");

                
                    


                var mood = ast.Mood;
                var trend = mood.CalculateMoodTrend();
                var time = mood.CalculateTimeToTarget(trend > 0 ? 100 : 0).ToString("F1");
                text.Line("Mood:");
                text.Bar(mood.MoodData.Value / 100f, $"{mood.MoodData.Value}/{100}");
                text.Append($"Trend: {trend.ToString("F1")} ({time}s.) ");

                
            }
        }

       
    }
}
