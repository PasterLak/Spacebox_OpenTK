using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Generation.Shapes;

public static class SupportedBlocks
{
    private static readonly Vector3i[] Neighbours =
    {
        new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0),
        new Vector3i(0, 1, 0), new Vector3i(0, -1, 0),
        new Vector3i(0, 0, 1), new Vector3i(0, 0, -1)
    };

    public static bool DependsOn(Vector3i cell, BlockShape shape, Direction direction, Rotation rotation, Vector3i supportCell)
    {
        if (shape == null || !shape.RequiresSupport) return false;
        var offset = shape.GetSupportOffset(direction, rotation);
        return cell + new Vector3i(offset.X, offset.Y, offset.Z) == supportCell;
    }

    public static void BreakAround(SpaceEntity entity, Vector3i removedCell)
    {
        if (entity == null) return;

        for (int i = 0; i < Neighbours.Length; i++)
        {
            var cell = removedCell + Neighbours[i];
            if (!entity.TryGetBlockLocal(cell, out var chunk, out var index)) continue;

            var block = chunk.Blocks[index.X, index.Y, index.Z];
            if (!BlockShapes.TryGet(block, out var shape)) continue;
            if (!DependsOn(cell, shape, block.Direction, block.Rotation, removedCell)) continue;
            if (BlockShapes.IsSupported(entity, cell, shape, block.Direction, block.Rotation)) continue;

            Break(entity, chunk, index, cell, block);
        }
    }

    private static void Break(SpaceEntity entity, Chunk chunk, Vector3Byte index, Vector3i cell, Block block)
    {
        block.OnDestroy?.Invoke(new HitInfo
        {
            chunk = chunk,
            block = block,
            blockPositionIndex = index,
            blockPositionEntity = cell,
            blockPositionWorld = entity.EntityLocalToWorld(new Vector3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z + 0.5f))
        });
        chunk.RemoveBlock(index.X, index.Y, index.Z, 0, 0, 0, true);
    }
}
