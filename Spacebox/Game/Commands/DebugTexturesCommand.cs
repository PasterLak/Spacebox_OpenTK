
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
            if(GameAssets.BlocksTexture != null)
            {
                GameAssets.BlocksTexture.SaveToPng("Debug/blocks.png",true);
            }
            if (GameAssets.ItemsTexture != null)
            {
                GameAssets.ItemsTexture.SaveToPng("Debug/items.png", true);
            }
            if (GameAssets.EmissionBlocks != null)
            {
                GameAssets.EmissionBlocks.SaveToPng("Debug/emissions.png", true);
            }

        }


    }
}
