using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Player.Building;

public static class PlacementBounds
{
    public static void Get(BlockData data, Direction direction, Rotation rotation, bool supported, List<MultiblockCell> cells, out Vector3 min, out Vector3 max)
    {
        cells.Clear();
        if (data.IsMultiblock) MultiblockLayout.GetCells(data, direction, rotation, cells);
        else cells.Add(new MultiblockCell(Vector3i.Zero, new Vector3Byte(0)));

        min = new Vector3(float.MaxValue);
        max = new Vector3(float.MinValue);
        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            GetCellBounds(data, cell.Model, direction, rotation, supported, out var cellMin, out var cellMax);
            var offset = new Vector3(cell.Offset.X, cell.Offset.Y, cell.Offset.Z);
            min = Vector3.ComponentMin(min, cellMin + offset);
            max = Vector3.ComponentMax(max, cellMax + offset);
        }
    }

    public static void GetSurfaceCorners(Vector3 min, Vector3 max, Vector3i surfaceNormal, float lift, Vector3[] corners)
    {
        int axis = surfaceNormal.X != 0 ? 0 : surfaceNormal.Y != 0 ? 1 : 2;
        int sign = surfaceNormal[axis] >= 0 ? 1 : -1;
        float plane = (sign > 0 ? min[axis] : max[axis]) + sign * lift;
        int u = (axis + 1) % 3;
        int v = (axis + 2) % 3;

        corners[0] = Corner(axis, plane, u, min[u], v, min[v]);
        corners[1] = Corner(axis, plane, u, max[u], v, min[v]);
        corners[2] = Corner(axis, plane, u, max[u], v, max[v]);
        corners[3] = Corner(axis, plane, u, min[u], v, max[v]);
    }

    private static void GetCellBounds(BlockData data, Vector3Byte model, Direction direction, Rotation rotation, bool supported, out Vector3 min, out Vector3 max)
    {
        if (data.Shape == null)
        {
            min = Vector3.Zero;
            max = Vector3.One;
            return;
        }

        var pose = data.Shape.ForCell(data.Size, model).GetPose(direction, rotation, supported);
        min = pose.Min;
        max = pose.Max;
    }

    private static Vector3 Corner(int axis, float plane, int u, float uValue, int v, float vValue)
    {
        var corner = Vector3.Zero;
        corner[axis] = plane;
        corner[u] = uValue;
        corner[v] = vValue;
        return corner;
    }
}
