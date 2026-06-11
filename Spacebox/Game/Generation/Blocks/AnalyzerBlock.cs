using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using System.Collections.Generic;

namespace Spacebox.Game.Generation.Blocks
{
    public class AnalyzerBlock : InteractiveBlock
    {
        public bool IsScanning = false;
        public bool ScanComplete = false;
        public long ScanStartAbsoluteTick = 0;
        public float TimePer1000Blocks = 0.5f;
        public float DeviationPercentage = 5f;

        public ulong CachedMass = 0;
        public Dictionary<short, int> CachedMinerals = new Dictionary<short, int>();

        public AnalyzerBlock(BlockData blockData) : base(blockData)
        {
            IsActive = false;
            EFlags = ElectricalFlags.CanConsume;
            MaxPower = 200;
            ConsumptionRate = 100;
            CurrentPower = 0;
        }

        public override void Use(LocalAstronaut player, ref HitInfo hit)
        {
            if (!IsActive) return;

            base.Use(player, ref hit);
            AnalyzerUI.Open(this, ref hit);
        }

        protected override void OnActiveStateChanged(bool isActive)
        {
            base.OnActiveStateChanged(isActive);

            if (IsScanning && !isActive)
            {
                IsScanning = false;
                ScanStartAbsoluteTick = 0;
            }
        }
    }
}