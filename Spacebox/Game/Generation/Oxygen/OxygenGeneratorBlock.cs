using Engine.Networking;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Oxygen;

public class OxygenGeneratorBlock : InteractiveBlock, IContainerBlock, IEntityBlockListener, INetworkStateBlock
{
    private SpaceEntity _entity;
    private OpenTK.Mathematics.Vector3i _cell;

    public void OnAddedToEntity(Chunk chunk, OpenTK.Mathematics.Vector3i localCell)
    {
        _entity = chunk.SpaceEntity;
        _cell = localCell;
    }

    public void OnRemovedFromEntity()
    {
        _entity = null;
    }

    public void WriteNetworkState(NetWriter writer) => writer.Write(Water);

    public void ReadNetworkState(NetReader reader) => Restore(reader.ReadInt(), ConsumeProgress);

    private void MarkStateDirty() => Spacebox.Network.BlockStateNetwork.MarkDirty(_entity, _cell, this);

    public const int Radius = 32;
    public const byte RequiredPower = 20;

    private readonly OxygenGeneratorBlockData _data;
    private float _fillTime;

    public Storage WaterStorage { get; } = new Storage(1, 1);
    public int Water { get; private set; }
    public float ConsumeProgress { get; private set; }

    public int TankCapacity => _data?.TankCapacity ?? 30;
    public float WaterConsumeInterval => _data?.WaterConsumeInterval ?? 120f;
    public bool HasWater => Water > 0;
    public bool CanRun => IsActive && HasWater;
    public bool IsOutside { get; private set; }
    public bool IsWorking => CanRun && !IsOutside;

    public void SetOutside(bool outside)
    {
        IsOutside = outside;
    }

    public OxygenGeneratorBlock(BlockData blockData) : base(blockData)
    {
        _data = blockData as OxygenGeneratorBlockData;

        EFlags = ElectricalFlags.CanConsume;
        MaxPower = 300;
        ConsumptionRate = RequiredPower;

        WaterStorage.Name = "Water";
        var slot = WaterStorage.GetSlot(0, 0);
        slot.Name = "Water";
        slot.Filter = IsWater;
        WaterStorage.OnDataWasChanged += _ => MarkModified();

        OnDestroy += DropWater;
    }

    public override void Use(LocalAstronaut player, ref HitInfo hit)
    {
        base.Use(player, ref hit);
        GameUi.Current?.OxygenGenerator.Open(this, player);
    }

    public string ContainerWindowId => "oxygenGenerator";

    public IReadOnlyList<Storage> GetContainerStorages() => [WaterStorage];

    public void WriteContainerState(NetWriter writer)
    {
        writer.Write(Water);
        writer.Write(ConsumeProgress);
    }

    public void ReadContainerState(NetReader reader)
    {
        int water = reader.ReadInt();
        float progress = reader.ReadFloat();
        Restore(water, progress);
    }

    public void OnContainerChangedByNetwork()
    {
    }

    public void Update(float deltaTime)
    {
        if (Spacebox.Network.GameSession.IsRemoteClient) return;

        FillTank(deltaTime);
        ConsumeWater(deltaTime);
    }

    public void Restore(int water, float consumeProgress)
    {
        SetWater(Math.Clamp(water, 0, TankCapacity));
        ConsumeProgress = Math.Clamp(consumeProgress, 0f, 1f);
    }

    private void FillTank(float deltaTime)
    {
        var slot = WaterStorage.GetSlot(0, 0);
        if (!slot.HasItem || Water >= TankCapacity)
        {
            _fillTime = 0f;
            return;
        }

        _fillTime += deltaTime;
        float interval = _data?.WaterFillInterval ?? 1f;
        if (_fillTime < interval) return;

        _fillTime -= interval;
        slot.TakeOne();
        Water++;
        MarkStateDirty();
        MarkModified();
    }

    private void ConsumeWater(float deltaTime)
    {
        if (!IsWorking) return;

        ConsumeProgress += deltaTime / WaterConsumeInterval;
        if (ConsumeProgress < 1f) return;

        ConsumeProgress -= 1f;
        SetWater(Water - 1);
        MarkStateDirty();
        MarkModified();
    }

    private void SetWater(int water)
    {
        bool ranDry = Water > 0 && water <= 0;
        Water = water;
        if (ranDry) PlayEmptySound();
    }

    private void PlayEmptySound()
    {
        if (_entity == null || _data == null) return;
        BlockEventSounds.Play(_data.EmptySound, _entity.EntityLocalToWorld(new OpenTK.Mathematics.Vector3(_cell.X + 0.5f, _cell.Y + 0.5f, _cell.Z + 0.5f)));
    }

    private bool IsWater(Item item)
    {
        var water = _data?.WaterItem;
        return water != null && item != null && item.Id == water.Id;
    }

    private void DropWater(HitInfo info)
    {
        if (!WaterStorage.HasAnyItems()) return;
        World.DropEffectManager?.DropStorage(WaterStorage, info.blockPositionWorld);
    }

    private void MarkModified()
    {
        if (chunk != null) chunk.IsModified = true;
    }
}
