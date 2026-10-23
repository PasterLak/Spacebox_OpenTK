using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks
{
    public class RadarBlock : InteractiveBlock
    {
        public RadarBlock(BlockData blockData) : base(blockData)
        {

            IsActive = false;
            EFlags = ElectricalFlags.CanConsume;
            MaxPower = 200;
            ConsumptionRate = 10;
            CurrentPower = 0;
        }

        public override void Use(LocalAstronaut player, ref HitInfo hit)
        {

            if (!IsActive) return;

            base.Use(player, ref hit);
            GameUi.Current?.Radar?.Open(this, player);
        }

        public override void TickElectric()
        {
            base.TickElectric();

            // SetEnableEmission(CurrentPower > 0);
        }
    }
}
