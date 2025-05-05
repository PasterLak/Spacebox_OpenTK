using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using Engine;
namespace Spacebox.Game.Generation.Blocks
{
    public class RadarBlock : InteractiveBlock
    {
        public RadarBlock(BlockData blockData) : base(blockData)
        {

            if (RadarUI.Instance != null)
                OnUse += RadarUI.Instance.Toggle;

            IsActive = false;
            EFlags = ElectricalFlags.CanConsume;
            MaxPower = 200;
            ConsumptionRate = 5;
            CurrentPower = 0;
        }

        public override void Use(Astronaut player)
        {

            if (!IsActive) return;

            base.Use(player);
        }

        public override void TickElectric()
        {
            base.TickElectric();

            // SetEnableEmission(CurrentPower > 0);
        }
    }
}
