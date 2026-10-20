using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks
{
    public class GeneratorBlock : InteractiveBlock, IExplosiveBlock
    {
        public ExplosiveBlockData Explosion { get; }

        public GeneratorBlock(BlockData blockData) : base(blockData)
        {
            Explosion = (blockData as GeneratorBlockData)?.Explosion;
            EFlags = ElectricalFlags.CanGenerate | ElectricalFlags.CanTransfer;
            ushort power = blockData is GeneratorBlockData generatorData ? generatorData.Power : GeneratorBlockData.DefaultPower;
            MaxPower = (short)Math.Clamp(power * 10, 500, 16000);
            GenerationRate = power;
            EnableEmission = true;


        }
        public override void TickElectric()
        {
            base.TickElectric();
            SetEnableEmission(CurrentPower > 0);
        }

        public override void Use(LocalAstronaut player, ref HitInfo hit)
        {
            base.Use(player, ref hit);

            var generator = hit.chunk?.SpaceEntity.GetStructureOrigin(hit.chunk, hit.blockPositionIndex) as GeneratorBlock ?? this;
            GeneratorUI.Open(generator, player, ref hit);
        }


    }
}
