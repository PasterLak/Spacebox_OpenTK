using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using System.Collections.Generic;

namespace Spacebox.Game.Generation.Blocks
{
    public class AnalyzerBlock : InteractiveBlock, IEntityBlockListener, INetworkStateBlock
    {
        public bool IsScanning = false;
        public bool ScanComplete = false;
        public long ScanStartAbsoluteTick = 0;
        public long ScanDurationTicks = 0;
        public float TimePer1000Blocks = 0.5f;
        public float DeviationPercentage = 5f;

        public ulong CachedMass = 0;
        public Dictionary<short, int> CachedMinerals = new Dictionary<short, int>();

        public SpaceEntity? Entity { get; private set; }
        public Vector3i LocalCell { get; private set; }

        public static long CurrentTick => GameTime.Day * 24000L + GameTime.DayTick;

        public float Progress => IsScanning && ScanDurationTicks > 0
            ? Math.Clamp((CurrentTick - ScanStartAbsoluteTick) / (float)ScanDurationTicks, 0f, 1f)
            : 0f;

        public AnalyzerBlock(BlockData blockData) : base(blockData)
        {
            IsActive = false;
            EFlags = ElectricalFlags.CanConsume;
            MaxPower = 200;
            ConsumptionRate = 70;
            CurrentPower = 0;
        }

        public override void Use(LocalAstronaut player, ref HitInfo hit)
        {
            if (!IsActive) return;

            base.Use(player, ref hit);
            GameUi.Current?.Analyzer.Open(this, ref hit);
        }

        public void OnAddedToEntity(Chunk chunk, Vector3i localCell)
        {
            this.chunk = chunk;
            Entity = chunk.SpaceEntity;
            LocalCell = localCell;
            AnalyzerRegistry.Add(this);
        }

        public void OnRemovedFromEntity()
        {
            AnalyzerRegistry.Remove(this);
            Entity = null;
        }

        public void MarkChanged()
        {
            if (chunk != null) chunk.IsModified = true;
            Spacebox.Network.BlockStateNetwork.MarkDirty(Entity, LocalCell, this);
        }

        public void WriteNetworkState(NetWriter writer)
        {
            writer.Write(IsScanning);
            writer.Write(IsScanning ? Math.Max(0L, CurrentTick - ScanStartAbsoluteTick) : 0L);
            writer.Write(ScanDurationTicks);
            writer.Write(ScanComplete);
            writer.Write(CachedMass);
            writer.Write(CachedMinerals.Count);
            foreach (var pair in CachedMinerals)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }

        public void ReadNetworkState(NetReader reader)
        {
            IsScanning = reader.ReadBool();
            long elapsed = reader.ReadLong();
            ScanDurationTicks = reader.ReadLong();
            ScanComplete = reader.ReadBool();
            CachedMass = reader.ReadULong();
            ScanStartAbsoluteTick = IsScanning ? CurrentTick - elapsed : 0;

            CachedMinerals.Clear();
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                short id = reader.ReadShort();
                CachedMinerals[id] = reader.ReadInt();
            }
        }

        protected override void OnActiveStateChanged(bool isActive)
        {
            base.OnActiveStateChanged(isActive);

            if (IsScanning && !isActive)
            {
                IsScanning = false;
                ScanStartAbsoluteTick = 0;
                MarkChanged();
            }
        }
    }
}
