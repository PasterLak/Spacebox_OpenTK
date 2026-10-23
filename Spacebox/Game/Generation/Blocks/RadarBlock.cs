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

            if (GameUi.Current?.Radar != null)
                OnUse += ToggleRadar;

            IsActive = false;
            EFlags = ElectricalFlags.CanConsume;
            MaxPower = 200;
            ConsumptionRate = 10;
            CurrentPower = 0;
        }

        private static void ToggleRadar(LocalAstronaut player)
        {
            GameUi.Current?.Radar?.Toggle(player);
        }

        public override void Use(LocalAstronaut player, ref HitInfo hit)
        {

            if (!IsActive) return;

            base.Use(player, ref hit);
        }

        public override void TickElectric()
        {
            base.TickElectric();

            // SetEnableEmission(CurrentPower > 0);
        }
    }
}
