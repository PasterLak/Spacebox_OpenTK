using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.GameMath;

namespace Spacebox.Game.Generation.Tools;

public static class BulkBlockEdit
{
    public const short EraseBlockId = 0;
    public const long MaxCells = 128L * 128L * 128L;
    public const int MinCoordinate = sbyte.MinValue * Chunk.Size;
    public const int MaxCoordinate = (sbyte.MaxValue + 1) * Chunk.Size - 1;

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
        return ((long)max.X - min.X + 1) * ((long)max.Y - min.Y + 1) * ((long)max.Z - min.Z + 1);
    }

    public static bool TryValidate(Vector3i min, Vector3i max, short blockId, Func<short, bool> blockExists, out string error)
    {
        error = null;

        if (blockId < 0 || (blockId != EraseBlockId && (blockExists == null || !blockExists(blockId))))
        {
            error = $"Unknown block id {blockId}";
            return false;
        }

        if (!InRange(min) || !InRange(max))
        {
            error = "The selection is outside the entity";
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
        if (!TryValidate(min, max, blockId, id => GameAssets.Blocks.ContainsKey(id), out error)) return false;
        if (blockId != EraseBlockId && BlockRemovalRules.NeedsFullRemoval(GameAssets.CreateBlockFromId(blockId)))
        {
            error = "This block cannot be used to fill an area";
            return false;
        }
        return true;
    }

    private static bool InRange(Vector3i v)
    {
        return v.X >= MinCoordinate && v.X <= MaxCoordinate && v.Y >= MinCoordinate && v.Y <= MaxCoordinate && v.Z >= MinCoordinate && v.Z <= MaxCoordinate;
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
            chunk.FinishBulkEdit();
    }
}
