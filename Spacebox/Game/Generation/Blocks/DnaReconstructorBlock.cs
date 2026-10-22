using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Player.Respawn;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public class DnaReconstructorBlock : InteractiveBlock, IEntityBlockListener, IContainerBlock, INetworkStateBlock
{
    private readonly DnaReconstructorBlockData _data;
    private int _syncedDnaCount = -1;

    public Storage DnaStorage { get; } = new Storage(1, 1);
    public SpaceEntity Entity { get; private set; }
    public Vector3i LocalCell { get; private set; }
    public Vector3 WorldCenter => Entity != null ? Entity.EntityLocalToWorld(new Vector3(LocalCell.X + 0.5f, LocalCell.Y + 0.5f, LocalCell.Z + 0.5f)) : Vector3.Zero;
    public DnaReconstructorBlockData Data => _data;

    public int DnaCount
    {
        get
        {
            var slot = DnaStorage.GetSlot(0, 0);
            return slot.HasItem ? slot.Count : 0;
        }
    }

    public int DnaCapacity => _data.DnaItem?.StackSize ?? 1;
    public bool HasDna => (Spacebox.Network.GameSession.IsRemoteClient && _syncedDnaCount >= 0 ? _syncedDnaCount : DnaCount) > 0;
    public bool IsReady => IsActive && HasDna && Entity != null;

    public DnaReconstructorBlock(BlockData blockData) : base(blockData)
    {
        _data = blockData as DnaReconstructorBlockData ?? new DnaReconstructorBlockData(blockData, null, string.Empty);

        EFlags = ElectricalFlags.CanConsume;
        ConsumptionRate = _data.Power;
        MaxPower = (short)Math.Clamp(_data.Power * 2, 300, 16000);

        DnaStorage.Name = "DNA";
        var slot = DnaStorage.GetSlot(0, 0);
        slot.Name = "DNA";
        slot.Filter = IsDna;
        DnaStorage.OnDataWasChanged += _ => OnDnaChanged();

        OnDestroy += DropDna;
    }

    public override void Use(LocalAstronaut player, ref HitInfo hit)
    {
        base.Use(player, ref hit);
        DnaReconstructorUI.Open(this, player);
    }

    public string ContainerWindowId => "dnaReconstructor";

    public IReadOnlyList<Storage> GetContainerStorages() => [DnaStorage];

    public void WriteContainerState(NetWriter writer)
    {
    }

    public void ReadContainerState(NetReader reader)
    {
    }

    public void OnContainerChangedByNetwork()
    {
    }

    public void WriteNetworkState(NetWriter writer)
    {
        writer.Write(DnaCount);
    }

    public void ReadNetworkState(NetReader reader)
    {
        _syncedDnaCount = reader.ReadInt();
    }

    public bool TryConsumeDna()
    {
        if (!IsReady) return false;

        DnaStorage.GetSlot(0, 0).TakeOne();
        MarkModified();
        return true;
    }

    public void OnAddedToEntity(Chunk chunk, Vector3i localCell)
    {
        this.chunk = chunk;
        Entity = chunk.SpaceEntity;
        LocalCell = localCell;
        DnaReconstructorRegistry.Register(this);
    }

    public void OnRemovedFromEntity()
    {
        DnaReconstructorRegistry.Unregister(this);
        Entity = null;
    }

    private bool IsDna(Item item)
    {
        var dna = _data.DnaItem;
        return dna != null && item != null && item.Id == dna.Id;
    }

    private void DropDna(HitInfo info)
    {
        if (!DnaStorage.HasAnyItems()) return;
        World.DropEffectManager?.DropStorage(DnaStorage, info.blockPositionWorld);
    }

    private void MarkModified()
    {
        if (chunk != null) chunk.IsModified = true;
    }

    private void OnDnaChanged()
    {
        MarkModified();
        DnaReconstructorRegistry.NotifyChanged(this);
        if (Entity != null) Spacebox.Network.BlockStateNetwork.MarkDirty(Entity, LocalCell, this);
    }

    protected override void OnActiveStateChanged(bool isActive)
    {
        base.OnActiveStateChanged(isActive);
        DnaReconstructorRegistry.NotifyChanged(this);
    }
}
