using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Logistics;

public static class ItemTransferEndpoints
{
    private static readonly List<Func<Block, IItemTransferEndpoint?>> _resolvers = new List<Func<Block, IItemTransferEndpoint?>>
    {
        block => block as IItemTransferEndpoint,
        block => block is StorageBlock storage ? new StorageEndpoint(storage.GetContainerStorages(), true, false) : null,
        block => block is ResourceProcessingBlock machine ? new MachineEndpoint(machine) : null,
        block => block is DisassemblerBlock disassembler ? new DisassemblerEndpoint(disassembler) : null,
        block => block is IContainerBlock container ? new StorageEndpoint(container.GetContainerStorages(), false, true) : null
    };

    public static void Register(Func<Block, IItemTransferEndpoint?> resolver)
    {
        _resolvers.Insert(1, resolver);
    }

    public static bool TryGet(Block? block, out IItemTransferEndpoint endpoint)
    {
        endpoint = null!;
        if (block == null || block.IsAir) return false;

        for (int i = 0; i < _resolvers.Count; i++)
        {
            var resolved = _resolvers[i](block);
            if (resolved == null) continue;

            endpoint = resolved;
            return true;
        }

        return false;
    }
}
