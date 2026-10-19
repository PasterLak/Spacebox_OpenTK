using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;

namespace Spacebox.Game.Physics;

public static class BlockCollision
{
    public static bool IsSolid(Block? block)
    {
        return block != null && !block.IsAir && !block.IsMeshHidden;
    }

    public static bool TryGetSolidBounds(SpaceEntity entity, Vector3i cell, out Vector3 min, out Vector3 max)
    {
        min = Vector3.Zero;
        max = Vector3.Zero;
        if (!entity.TryGetBlockLocal(cell, out var chunk, out var index)) return false;

        var block = chunk.Blocks[index.X, index.Y, index.Z];
        if (!IsSolid(block)) return false;

        GetCellBounds(entity, cell, block, out min, out max);
        var origin = new Vector3(cell.X, cell.Y, cell.Z);
        min += origin;
        max += origin;
        return true;
    }

    public static void GetChunkCellBounds(Chunk chunk, int x, int y, int z, Block block, out Vector3 min, out Vector3 max)
    {
        var cell = new Vector3i(chunk.PositionIndex.X * Chunk.Size + x, chunk.PositionIndex.Y * Chunk.Size + y, chunk.PositionIndex.Z * Chunk.Size + z);
        GetCellBounds(chunk.SpaceEntity, cell, block, out min, out max);
        min += new Vector3(x, y, z);
        max += new Vector3(x, y, z);
    }

    public static void GetCellBounds(SpaceEntity? entity, Vector3i cell, Block block, out Vector3 min, out Vector3 max)
    {
        if (!BlockShapes.TryGet(block, out var shape))
        {
            min = Vector3.Zero;
            max = Vector3.One;
            return;
        }

        bool supported = BlockShapes.IsSupported(entity, cell, shape, block.Direction, block.Rotation);
        var pose = shape.GetPose(block.Direction, block.Rotation, supported);
        min = pose.Min;
        max = pose.Max;
    }
}
