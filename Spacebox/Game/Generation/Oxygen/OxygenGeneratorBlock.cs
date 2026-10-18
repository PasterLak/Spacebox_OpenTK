using Engine.Networking;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Oxygen;

public class OxygenGeneratorBlock : InteractiveBlock, IContainerBlock
{
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
    public bool IsWorking => IsActive && HasWater;

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
        OxygenGeneratorUI.Open(this, player);
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
        Water = Math.Clamp(water, 0, TankCapacity);
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
        MarkModified();
    }

    private void ConsumeWater(float deltaTime)
    {
        if (!IsWorking) return;

        ConsumeProgress += deltaTime / WaterConsumeInterval;
        if (ConsumeProgress < 1f) return;

        ConsumeProgress -= 1f;
        Water--;
        MarkModified();
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
