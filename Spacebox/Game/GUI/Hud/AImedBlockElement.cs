using Engine.GUI;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.GUI
{
    public class AImedBlockElement : OverlayElement
    {
       public static Block? AimedBlock = null;

        public override void Write(OverlayText text)
        {
            if (AimedBlock != null)
            {

                text.Section("[BLOCK]");
                text.Line(AimedBlock.ToString());
              
                //if(AimedBlock.Is<St>)

                AimedBlock = null;
            }
        }
    }
}
