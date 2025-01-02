using ImGuiNET;
using Spacebox.Common;
using Spacebox.Common.GUI;
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

                if (ast != null)
                {
                    string formatted = ast.InertiaController.Velocity.Length.ToString("0.0");
                    ImGui.Text($"Speed: {formatted}");
                }
                
                ImGui.Text($"Gamemode: {ast.GameMode}");
                
                if(ast.CurrentInteraction != null)
                ImGui.Text($"Interaction: {ast.CurrentInteraction.GetType().Name}");
            }
        }

       
    }
}
