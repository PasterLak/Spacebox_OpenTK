using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Shapes;

public static class BlockModelMesh
{
    public const Direction FlatFacing = Direction.Forward;

    public static (float[] vertices, uint[] indices) Build(BlockData data, BlockState state)
    {
        var shape = data.Shape;
        if (shape == null) return MultiblockPreviewMesh.Build(data, state);

        var direction = shape.HasFacing ? FlatFacing : Block.DefaultDirection;
        var rotation = shape.HasFacing ? shape.BestRotation(direction, Vector3.UnitY) : Rotation.None;
        return BlockShapePreviewMesh.Build(data, shape, direction, rotation, true, state);
    }
}
