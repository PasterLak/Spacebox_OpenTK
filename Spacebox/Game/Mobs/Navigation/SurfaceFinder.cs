using OpenTK.Mathematics;

namespace Spacebox.Game.Mobs.Navigation;

public static class SurfaceFinder
{
    private const int DefaultAttempts = 24;

    private static readonly Vector3i[] Faces =
    {
        new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0),
        new Vector3i(0, 1, 0), new Vector3i(0, -1, 0),
        new Vector3i(0, 0, 1), new Vector3i(0, 0, -1)
    };

    public static bool IsSurfaceCell(MobNavigationGrid grid, Vector3i cell)
    {
        if (!grid.IsPassable(cell)) return false;

        for (int i = 0; i < Faces.Length; i++)
        {
            if (grid.IsSolid(cell + Faces[i])) return true;
        }
        return false;
    }

    public static bool IsOnSurface(MobNavigationGrid grid, Vector3 worldPosition)
    {
        return IsSurfaceCell(grid, grid.WorldToCell(worldPosition));
    }

    public static bool TryFind(MobNavigationGrid grid, Vector3 worldOrigin, float minDistance, float maxDistance, Random random, out Vector3 worldPosition, int attempts = DefaultAttempts)
    {
        var origin = grid.WorldToCell(worldOrigin);

        for (int i = 0; i < attempts; i++)
        {
            var offset = MobRandom.Direction(random) * MobRandom.Range(random, minDistance, maxDistance);
            var cell = origin + new Vector3i((int)MathF.Round(offset.X), (int)MathF.Round(offset.Y), (int)MathF.Round(offset.Z));
            if (!IsSurfaceCell(grid, cell)) continue;

            worldPosition = grid.CellToWorld(cell);
            return true;
        }

        worldPosition = default;
        return false;
    }
}
