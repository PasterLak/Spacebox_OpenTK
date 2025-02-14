
using Engine.Commands;

namespace Spacebox.Game.Commands
{
    internal class DebugTexturesCommand : ICommand
    {
        public string Name => "save_atlas";

        public string Description => "say to all";


        public DebugTexturesCommand()
        {

        }
        public void Execute(string[] args)
        {
            if (!Directory.Exists("Debug"))
            {
                Directory.CreateDirectory("Debug");
            }
            if(GameBlocks.BlocksTexture != null)
            {
                GameBlocks.BlocksTexture.SaveToPng("Debug/blocks.png",true);
            }
            if (GameBlocks.ItemsTexture != null)
            {
                GameBlocks.ItemsTexture.SaveToPng("Debug/items.png", true);
            }
            if (GameBlocks.LightAtlas != null)
            {
                GameBlocks.LightAtlas.SaveToPng("Debug/emissions.png", true);
            }

        }


    }
}
