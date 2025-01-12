using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
{

        public class DisassemblerBlock : ResourceProcessingBlock
        {

            public DisassemblerBlock(BlockData blockData) : base(blockData)
            {
                OnUse += ResourceProcessingGUI.Toggle;
                WindowName = "Disassembler";
            SetEmissionWithoutRedrawChunk(false);
        }

            public override void Use(Astronaut player)
            {
             
                base.Use(player);
            ResourceProcessingGUI.Activate(this, player);
        }


        }
    

}
