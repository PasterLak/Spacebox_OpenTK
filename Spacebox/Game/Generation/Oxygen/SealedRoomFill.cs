using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation.Oxygen;

public sealed class SealedRoomFill
{
    private static readonly Vector3i[] _directions =
    {
        Vector3i.UnitX, -Vector3i.UnitX, Vector3i.UnitY, -Vector3i.UnitY, Vector3i.UnitZ, -Vector3i.UnitZ
    };

    private readonly Queue<Vector3i> _queue = new Queue<Vector3i>();

    public int CellCount { get; private set; }

    public bool Fill(SpaceEntity entity, Vector3i origin, int radius, int maxCells, AirMasks room)
    {
        room.Clear();
        _queue.Clear();
        CellCount = 0;

        bool isSealed = true;
        for (int d = 0; d < _directions.Length && isSealed; d++)
            isSealed = TryVisit(entity, origin, origin + _directions[d], radius, maxCells, room);

        while (isSealed && _queue.Count > 0)
        {
            var cell = _queue.Dequeue();
            for (int d = 0; d < _directions.Length && isSealed; d++)
                isSealed = TryVisit(entity, origin, cell + _directions[d], radius, maxCells, room);
        }

        _queue.Clear();
        if (isSealed && CellCount > 0) return true;

        room.Clear();
        CellCount = 0;
        return false;
    }

    private bool TryVisit(SpaceEntity entity, Vector3i origin, Vector3i cell, int radius, int maxCells, AirMasks room)
    {
        if (room.Contains(cell.X, cell.Y, cell.Z)) return true;

        var offset = cell - origin;
        if ((long)offset.X * offset.X + (long)offset.Y * offset.Y + (long)offset.Z * offset.Z > (long)radius * radius) return false;
        if (!AirGrid.IsInsideEntity(cell.X, cell.Y, cell.Z)) return false;
        if (!entity.TryGetBlockLocal(cell, out var chunk, out var index)) return false;

        var block = chunk.Blocks[index.X, index.Y, index.Z];
        if (!IsPassable(block)) return true;

        if (++CellCount > maxCells) return false;

        room.Add(cell.X, cell.Y, cell.Z);
        _queue.Enqueue(cell);
        return true;
    }

    private static bool IsPassable(Block block)
    {
        return block == null || block.IsAir || block.HasCustomShape;
    }
}
