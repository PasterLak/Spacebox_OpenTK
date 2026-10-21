using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Doors;

namespace Spacebox.Game.Generation.Tools;

public static class BlockRemovalRules
{
    public static bool NeedsFullRemoval(Block block)
    {
        if (block == null) return false;
        if (block.OnDestroy != null || block is ElectricalBlock || block is IEntityBlockListener || block is DoorBlock)
            return true;

        return GameAssets.Blocks.TryGetValue(block.Id, out var data) && data.IsMultiblock;
    }
}
