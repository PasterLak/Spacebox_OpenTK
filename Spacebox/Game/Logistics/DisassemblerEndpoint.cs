using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Logistics;

public sealed class DisassemblerEndpoint : IItemTransferEndpoint
{
    private readonly DisassemblerBlock _disassembler;

    public DisassemblerEndpoint(DisassemblerBlock disassembler)
    {
        _disassembler = disassembler;
    }

    public ItemSlot? FindExtractable(Predicate<Item> accept)
    {
        return StorageTransfer.FindItem(_disassembler.OutputStorage, accept);
    }

    public void OnExtracted()
    {
        _disassembler.OnContainerChangedByNetwork();
    }

    public bool CanInsert(Item item, Vector3i fromSide)
    {
        return FindInputSlot(item) != null;
    }

    public bool Insert(Item item, Vector3i fromSide)
    {
        var slot = FindInputSlot(item);
        if (slot == null || !StorageTransfer.InsertOne(slot, item)) return false;

        _disassembler.OnContainerChangedByNetwork();
        return true;
    }

    private ItemSlot? FindInputSlot(Item item)
    {
        if (!_disassembler.Inventory.CanDisassemble(item)) return null;
        return StorageTransfer.FindSlotFor(_disassembler.InputStorage, item, false);
    }
}
