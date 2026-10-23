using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public static class UnbreakableBlocks
{
    public static bool Is(Block block) => block != null && Is(block.Id);

    public static bool Is(short blockId) => GameAssets.Blocks.TryGetValue(blockId, out var data) && data.Unbreakable;

    public static bool CanDestroy(Block block, bool breakUnbreakable) => breakUnbreakable || !Is(block);
}
