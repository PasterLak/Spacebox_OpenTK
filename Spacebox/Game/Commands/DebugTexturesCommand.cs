
using Engine.Commands;

namespace Spacebox.Game.Commands
{
    internal class DebugTexturesCommand : CommandBase
    {
        public override string Name => "save_atlas";

        public override string Description => "Save atlas textures to Debug folder";


        public DebugTexturesCommand()
        {

        }
        public override void Execute(string[] args)
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
