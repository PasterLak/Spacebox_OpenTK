using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks
{
    public class GeneratorBlock : InteractiveBlock
    {
        public GeneratorBlock(BlockData blockData) : base(blockData)
        {
            EFlags = ElectricalFlags.CanGenerate | ElectricalFlags.CanTransfer;
            MaxPower = 500;
            GenerationRate = 50;
            EnableEmission = true;

            

        }
        public override void TickElectric()
        {
            base.TickElectric();
            SetEnableEmission(CurrentPower > 0);
        }

        public override void Use(Astronaut player, ref HitInfo hit)
        {
            base.Use(player, ref hit);

            GeneratorUI.Open(this, player, ref hit);
        }


    }
}
