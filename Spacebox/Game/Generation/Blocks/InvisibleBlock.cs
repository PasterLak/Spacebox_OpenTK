using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public class InvisibleBlock : Block
{
    public const string TypeName = "invisible";

    public InvisibleBlock(BlockData blockData) : base(blockData)
    {
    }

    public static bool IsInvisibleType(short blockId) =>
        GameAssets.Blocks.TryGetValue(blockId, out var data) && string.Equals(data.Type, TypeName, StringComparison.OrdinalIgnoreCase);
}
