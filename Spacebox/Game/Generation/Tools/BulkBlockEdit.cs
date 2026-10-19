using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.GameMath;

namespace Spacebox.Game.Generation.Tools;

public static class BulkBlockEdit
{
    public const short EraseBlockId = 0;
    public const long MaxCells = 128L * 128L * 128L;

    public static void Normalize(ref Vector3i min, ref Vector3i max)
    {
        var a = min;
        var b = max;
        min = Vector3i.ComponentMin(a, b);
        max = Vector3i.ComponentMax(a, b);
    }

    public static long CountCells(Vector3i min, Vector3i max)
    {
        Normalize(ref min, ref max);
        return (long)(max.X - min.X + 1) * (max.Y - min.Y + 1) * (max.Z - min.Z + 1);
    }

    public static bool TryValidate(Vector3i min, Vector3i max, short blockId, Func<short, bool> blockExists, out string error)
    {
        error = null;

        if (blockId < 0 || (blockId != EraseBlockId && (blockExists == null || !blockExists(blockId))))
        {
            error = $"Unknown block id {blockId}";
            return false;
        }

        if (CountCells(min, max) > MaxCells)
        {
            error = $"The selection is too large (more than {MaxCells} blocks)";
            return false;
        }

        return true;
    }

    public static bool TryValidate(Vector3i min, Vector3i max, short blockId, out string error)
    {
        return TryValidate(min, max, blockId, id => GameAssets.Blocks.ContainsKey(id), out error);
    }

    public static void Apply(SpaceEntity entity, Vector3i min, Vector3i max, short blockId)
    {
        if (entity == null) return;

        Normalize(ref min, ref max);
        var box = BoundingBox.CreateFromMinMax(new Vector3(min.X, min.Y, min.Z), new Vector3(max.X, max.Y, max.Z));

        var chunks = blockId == EraseBlockId
            ? SpaceMath.Entity.RemoveBlocksInLocalBox(entity, box)
            : SpaceMath.Entity.FillBlocksInLocalBox(entity, box, blockId);

        foreach (var chunk in chunks)
            chunk.QueueMeshUpdate();
    }
}
