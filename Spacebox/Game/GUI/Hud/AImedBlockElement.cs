using ImGuiNET;
using Engine.GUI;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.GUI
{
    public class AImedBlockElement : OverlayElement
    {
       public static Block? AimedBlock = null;

        public override void OnGUIText()
        {
            if (AimedBlock != null)
            {

                ImGui.SeparatorText("[BLOCK]");
                ImGui.Text(AimedBlock.ToString());
              
                //if(AimedBlock.Is<St>)

                AimedBlock = null;
            }
        }
    }
}
