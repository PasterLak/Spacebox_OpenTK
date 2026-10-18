using OpenTK.Mathematics;

namespace Engine.AI;

public static class VoxelLine
{
    public const int DefaultMaxSteps = 1024;

    public static Vector3i ToCell(Vector3 position)
    {
        return new Vector3i((int)MathF.Floor(position.X), (int)MathF.Floor(position.Y), (int)MathF.Floor(position.Z));
    }

    public static Vector3 CellCenter(Vector3i cell)
    {
        return new Vector3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z + 0.5f);
    }

    public static bool IsClear<TGrid>(TGrid grid, Vector3 from, Vector3 to, int maxSteps = DefaultMaxSteps) where TGrid : INavigationGrid
    {
        var cell = ToCell(from);
        var end = ToCell(to);
        if (cell == end) return true;

        var delta = to - from;
        int stepX = Math.Sign(delta.X);
        int stepY = Math.Sign(delta.Y);
        int stepZ = Math.Sign(delta.Z);

        float tDeltaX = stepX != 0 ? 1f / MathF.Abs(delta.X) : float.MaxValue;
        float tDeltaY = stepY != 0 ? 1f / MathF.Abs(delta.Y) : float.MaxValue;
        float tDeltaZ = stepZ != 0 ? 1f / MathF.Abs(delta.Z) : float.MaxValue;

        float tMaxX = stepX != 0 ? Boundary(from.X, cell.X, stepX) * tDeltaX : float.MaxValue;
        float tMaxY = stepY != 0 ? Boundary(from.Y, cell.Y, stepY) * tDeltaY : float.MaxValue;
        float tMaxZ = stepZ != 0 ? Boundary(from.Z, cell.Z, stepZ) * tDeltaZ : float.MaxValue;

        for (int step = 0; step < maxSteps; step++)
        {
            if (tMaxX < tMaxY && tMaxX < tMaxZ)
            {
                if (tMaxX > 1f) return true;
                cell.X += stepX;
                tMaxX += tDeltaX;
            }
            else if (tMaxY < tMaxZ)
            {
                if (tMaxY > 1f) return true;
                cell.Y += stepY;
                tMaxY += tDeltaY;
            }
            else
            {
                if (tMaxZ > 1f) return true;
                cell.Z += stepZ;
                tMaxZ += tDeltaZ;
            }

            if (!grid.IsPassable(cell)) return false;
            if (cell == end) return true;
        }

        return false;
    }

    private static float Boundary(float position, int cell, int step)
    {
        return step > 0 ? cell + 1 - position : position - cell;
    }
}
