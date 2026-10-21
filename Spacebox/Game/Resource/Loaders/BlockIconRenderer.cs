using Engine.Graphics.Icons;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;

namespace Spacebox.Game.Resource;

public static class BlockIconRenderer
{
    public const int Size = 64;
    public const Direction FlatFacing = Direction.Forward;

    public static bool NeedsModelIcon(BlockData data) => data.Shape != null || data.IsMultiblock;

    public static Color4[,] Render(BlockData data)
    {
        var (vertices, indices) = BuildMesh(data);
        return IsometricRasterizer.Render(vertices, indices, Size, GameAssets.AtlasBlocks.TrySample);
    }

    private static (float[] vertices, uint[] indices) BuildMesh(BlockData data)
    {
        var shape = data.Shape;
        if (shape == null) return MultiblockPreviewMesh.Build(data, BlockState.Active);

        var direction = shape.HasFacing ? FlatFacing : Block.DefaultDirection;
        var rotation = shape.HasFacing ? shape.BestRotation(direction, Vector3.UnitY) : Rotation.None;
        return BlockShapePreviewMesh.Build(data, shape, direction, rotation, true, BlockState.Active);
    }
}
