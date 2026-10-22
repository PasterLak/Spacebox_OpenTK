using Engine.Graphics.Icons;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;

namespace Spacebox.Game.Resource;

public static class BlockIconRenderer
{
    public const int Size = 64;
    public const Direction FlatFacing = BlockModelMesh.FlatFacing;

    public static bool NeedsModelIcon(BlockData data) => data.Shape != null || data.IsMultiblock;

    public static Color4[,] Render(BlockData data)
    {
        var (vertices, indices) = BlockModelMesh.Build(data, BlockState.Active);
        return IsometricRasterizer.Render(vertices, indices, Size, GameAssets.AtlasBlocks.TrySample);
    }
}
