using ImGuiNET;
using Spacebox.Common.GUI;
using Spacebox.Game.Generation;

namespace Spacebox.Game.GUI
{
    public class AImedBlockElement : OverlayElement
    {
        public static Block? AimedBlock = null;

        public override void OnGUIText()
        {
            if (AimedBlock != null)
            {
                ImGui.Text($" ");
                ImGui.Text($"Block");
                ImGui.Text(AimedBlock.ToString());
            }
        }
    }
}
