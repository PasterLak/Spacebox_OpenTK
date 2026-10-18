using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Logistics;

public sealed class MachineEndpoint : IItemTransferEndpoint
{
    private readonly ResourceProcessingBlock _machine;

    public MachineEndpoint(ResourceProcessingBlock machine)
    {
        _machine = machine;
    }

    public ItemSlot? FindExtractable(Predicate<Item> accept)
    {
        var output = _machine.OutputStorage.GetSlot(0, 0);
        return output.HasItem && accept(output.Item) ? output : null;
    }

    public void OnExtracted()
    {
        _machine.OnContainerChangedByNetwork();
    }

    public bool CanInsert(Item item, Vector3i fromSide)
    {
        if (!GameAssets.TryGetRecipe(_machine.BlockType, item.Id, out _)) return false;

        var input = _machine.InputStorage.GetSlot(0, 0);
        return !input.HasItem || (input.Item.Id == item.Id && input.Count < item.StackSize);
    }

    public bool Insert(Item item, Vector3i fromSide)
    {
        if (!CanInsert(item, fromSide)) return false;
        if (!StorageTransfer.InsertOne(_machine.InputStorage.GetSlot(0, 0), item)) return false;

        _machine.OnContainerChangedByNetwork();
        return true;
    }
}
