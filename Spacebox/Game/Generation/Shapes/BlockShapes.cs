using System.Diagnostics.CodeAnalysis;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation.Shapes;

public static class BlockShapes
{
    public static bool TryGet(Block? block, [NotNullWhen(true)] out BlockShape? shape)
    {
        shape = null;
        if (block == null || !block.HasCustomShape) return false;
        if (!GameAssets.Blocks.TryGetValue(block.Id, out var data)) return false;

        shape = data.Shape;
        return shape != null;
    }

    public static bool CanSupport(Block? block)
    {
        return block != null && !block.IsAir && !block.IsMeshHidden && !block.HasCustomShape;
    }

    public static bool IsSupported(SpaceEntity? entity, Vector3i cell, BlockShape shape, Direction direction, Rotation rotation)
    {
        if (entity == null) return false;

        var offset = shape.GetSupportOffset(direction, rotation);
        var supportCell = cell + new Vector3i(offset.X, offset.Y, offset.Z);
        return entity.TryGetBlockLocal(supportCell, out var chunk, out var index) &&
               CanSupport(chunk.Blocks[index.X, index.Y, index.Z]);
    }
}
