using OpenTK.Mathematics;
using SharpNBT;

namespace Spacebox.Game.Ships.Sharing;

public sealed class ShipCellSet
{
    private readonly HashSet<Vector3i> _cells;

    public ShipCellSet(IEnumerable<Vector3i> cells)
    {
        _cells = new HashSet<Vector3i>(cells);
    }

    public int Count => _cells.Count;

    public static ShipCellSet FromEntity(CompoundTag entity)
    {
        var cells = new List<Vector3i>();
        foreach (var cell in ShipPreviewCells.Read(entity, _ => 0)) cells.Add(cell.Cell);
        return new ShipCellSet(cells);
    }

    public bool Contains(Vector3 localPoint)
    {
        return _cells.Contains(new Vector3i((int)MathF.Floor(localPoint.X), (int)MathF.Floor(localPoint.Y), (int)MathF.Floor(localPoint.Z)));
    }

    public bool ContainsNear(Vector3 localPoint, float margin)
    {
        if (Contains(localPoint)) return true;
        if (margin <= 0f) return false;

        for (int axis = 0; axis < 3; axis++)
        {
            var offset = Vector3.Zero;
            offset[axis] = margin;
            if (Contains(localPoint + offset) || Contains(localPoint - offset)) return true;
        }
        return false;
    }
}
