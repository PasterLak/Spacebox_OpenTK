using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Logistics;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public class PipeBlock : InteractiveBlock, IEntityBlockListener, IContainerBlock, IItemTransferEndpoint
{
    private const string FilterHint = "Put any item here: the pipe then moves only this item.\nEmpty: the pipe moves everything it can.\nThe filter item itself is never moved.";

    private readonly PipeBlockData _data;
    private readonly List<IItemTransferEndpoint> _outputs = new List<IItemTransferEndpoint>(5);
    private readonly List<Vector3i> _outputSides = new List<Vector3i>(5);
    private readonly List<Vector3i> _outputDirections = new List<Vector3i>(5);
    private readonly Predicate<Item> _canPull;
    private float _timer;
    private int _nextOutput;

    public Storage Storage { get; } = new Storage(2, 1);
    public ItemSlot Buffer => Storage.GetSlot(0, 0)!;
    public ItemSlot FilterSlot => Storage.GetSlot(1, 0)!;
    public SpaceEntity? Entity { get; private set; }
    public Vector3i LocalCell { get; private set; }
    public Vector3i InputSide { get; private set; } = new Vector3i(0, -1, 0);

    public PipeBlock(BlockData blockData) : base(blockData)
    {
        _data = blockData as PipeBlockData ?? new PipeBlockData(blockData, null);
        _canPull = CanPull;

        EFlags = ElectricalFlags.CanConsume;
        ConsumptionRate = _data.Power;
        MaxPower = (short)Math.Clamp(_data.Power * 10, 50, 16000);

        Storage.Name = "Pipe";
        Buffer.Name = "Item";
        FilterSlot.Name = "Filter";
        FilterSlot.Hint = FilterHint;
        FilterSlot.IsManualOnly = true;
        Storage.OnDataWasChanged += _ => MarkModified();

        OnDestroy += DropItems;
    }

    public override void Use(LocalAstronaut player, ref HitInfo hit)
    {
        base.Use(player, ref hit);
        StorageUI.OpenStorage(Storage, player);
    }

    public string ContainerWindowId => "storage";

    public IReadOnlyList<Storage> GetContainerStorages() => [Storage];

    public void WriteContainerState(NetWriter writer)
    {
    }

    public void ReadContainerState(NetReader reader)
    {
    }

    public void OnContainerChangedByNetwork()
    {
    }

    public void OnAddedToEntity(Chunk chunk, Vector3i localCell)
    {
        this.chunk = chunk;
        Entity = chunk.SpaceEntity;
        LocalCell = localCell;
        CalculateSides();
        PipeRegistry.Add(this);
    }

    public void OnRemovedFromEntity()
    {
        PipeRegistry.Remove(this);
        Entity = null;
    }

    public void UpdateTransfer(float deltaTime)
    {
        if (Entity == null || !IsActive) return;

        _timer += deltaTime;
        if (_timer < _data.TransferInterval) return;
        _timer = 0f;

        CollectOutputs();
        if (!Buffer.HasItem) Pull();
        if (Buffer.HasItem) Push();
    }

    public bool AllowsItem(Item item)
    {
        return !FilterSlot.HasItem || FilterSlot.Item.Id == item.Id;
    }

    public ItemSlot? FindExtractable(Predicate<Item> accept)
    {
        return null;
    }

    public void OnExtracted()
    {
    }

    public bool CanInsert(Item item, Vector3i fromSide)
    {
        return fromSide == InputSide && !Buffer.HasItem && AllowsItem(item);
    }

    public bool Insert(Item item, Vector3i fromSide)
    {
        if (!CanInsert(item, fromSide)) return false;

        Buffer.SetData(item, 1);
        return true;
    }

    private void Pull()
    {
        if (_outputs.Count == 0 || !TryGetEndpoint(InputSide, out var source)) return;

        var slot = source.FindExtractable(_canPull);
        if (slot == null) return;

        Buffer.SetData(slot.Item, 1);
        slot.TakeOne();
        source.OnExtracted();
    }

    private void Push()
    {
        var item = Buffer.Item;
        for (int i = 0; i < _outputs.Count; i++)
        {
            int index = (_nextOutput + i) % _outputs.Count;
            if (!_outputs[index].Insert(item, -_outputSides[index])) continue;

            Buffer.TakeOne();
            _nextOutput = index + 1;
            return;
        }
    }

    private bool CanPull(Item item)
    {
        if (!AllowsItem(item)) return false;

        for (int i = 0; i < _outputs.Count; i++)
        {
            if (_outputs[i].CanInsert(item, -_outputSides[i])) return true;
        }
        return false;
    }

    private void CollectOutputs()
    {
        _outputs.Clear();
        _outputSides.Clear();

        for (int i = 0; i < _outputDirections.Count; i++)
        {
            var side = _outputDirections[i];
            if (!TryGetEndpoint(side, out var endpoint)) continue;

            _outputs.Add(endpoint);
            _outputSides.Add(side);
        }
    }

    private bool TryGetEndpoint(Vector3i side, out IItemTransferEndpoint endpoint)
    {
        endpoint = null!;
        var entity = Entity;
        if (entity == null || !entity.TryGetBlockLocal(LocalCell + side, out var chunk, out var index)) return false;

        var block = chunk.Blocks[index.X, index.Y, index.Z];
        if (block != null && block.HasMultiblockOffset) block = entity.GetStructureOrigin(chunk, index);

        return ItemTransferEndpoints.TryGet(block, out endpoint);
    }

    private void CalculateSides()
    {
        var transform = BlockRotationHelper.CalculateTransformMatrix(GameAssets.GetBaseFrontDirection(Id), Direction, Rotation);
        InputSide = Rotate(transform, -Vector3.UnitY);

        _outputDirections.Clear();
        for (int i = 0; i < _data.OutputSides.Count; i++)
            _outputDirections.Add(Rotate(transform, BlockRotationHelper.GetDirectionVector(_data.OutputSides[i])));
    }

    private static Vector3i Rotate(Matrix4 transform, Vector3 direction)
    {
        var rotated = Vector3.TransformVector(direction, transform);
        return new Vector3i((int)MathF.Round(rotated.X), (int)MathF.Round(rotated.Y), (int)MathF.Round(rotated.Z));
    }


    private void MarkModified()
    {
        if (chunk != null) chunk.IsModified = true;
    }

    private void DropItems(HitInfo info)
    {
        if (!Storage.HasAnyItems()) return;
        World.DropEffectManager?.DropStorage(Storage, info.blockPositionWorld);
    }
}
