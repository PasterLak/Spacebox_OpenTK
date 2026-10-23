using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks.Components;

namespace Spacebox.Game.Logistics;

public sealed class MachineEndpoint : IItemTransferEndpoint
{
    private readonly ProcessingComponent _machine;

    public MachineEndpoint(ProcessingComponent machine)
    {
        _machine = machine;
    }

    public ItemSlot? FindExtractable(Predicate<Item> accept)
    {
        var output = _machine.OutputStorage.GetSlot(0, 0);
        if (output.HasItem && accept(output.Item)) return output;
        return FindInBuffer(accept);
    }

    private ItemSlot? FindInBuffer(Predicate<Item> accept)
    {
        var buffer = _machine.BufferStorage;
        if (buffer == null) return null;

        for (int x = 0; x < buffer.SizeX; x++)
        {
            for (int y = 0; y < buffer.SizeY; y++)
            {
                var slot = buffer.GetSlot(x, y);
                if (slot.HasItem && accept(slot.Item) && !GameAssets.TryGetRecipe(_machine.RecipeType, slot.Item.Id, out _)) return slot;
            }
        }

        return null;
    }

    public void OnExtracted()
    {
        _machine.OnContainerChangedByNetwork();
    }

    public bool CanInsert(Item item, Vector3i fromSide)
    {
        if (!GameAssets.TryGetRecipe(_machine.RecipeType, item.Id, out _)) return false;

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
