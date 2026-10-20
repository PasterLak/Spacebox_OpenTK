using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Physics.Explosions;

public sealed class BlastRayCaster
{
    private const float Step = 0.5f;
    private const int MinRays = 200;
    private const int MaxRays = 4000;
    private const float RaysPerSquareUnit = 1.5f;

    private readonly Dictionary<int, Vector3[]> _directionsByCount = new Dictionary<int, Vector3[]>();

    public void Collect(SpaceEntity entity, Vector3 centerLocal, float radius, int penetration, HashSet<Vector3i> cells, IReadOnlyList<Vector3i> sourceCells = null)
    {
        cells.Clear();
        cells.Add(ToCell(centerLocal));
        if (sourceCells != null)
        {
            for (int i = 0; i < sourceCells.Count; i++) cells.Add(sourceCells[i]);
        }

        var directions = GetDirections(radius);
        int steps = (int)MathF.Ceiling(radius / Step);

        for (int r = 0; r < directions.Length; r++)
        {
            var direction = directions[r];
            float power = penetration;
            var lastCell = ToCell(centerLocal);

            for (int s = 1; s <= steps; s++)
            {
                var cell = ToCell(centerLocal + direction * (s * Step));
                if (cell == lastCell) continue;
                lastCell = cell;

                if (!TryGetDurability(entity, cell, out int durability)) continue;
                if (cells.Contains(cell)) continue;
                if (power < durability) break;

                power -= durability;
                cells.Add(cell);
            }
        }
    }

    private static bool TryGetDurability(SpaceEntity entity, Vector3i cell, out int durability)
    {
        durability = 0;
        if (!entity.TryGetBlockLocal(cell, out var chunk, out var index)) return false;

        var block = chunk.Blocks[index.X, index.Y, index.Z];
        if (block == null || block.IsAir) return false;

        durability = Math.Max(1, (int)block.Durability);
        return true;
    }

    private static Vector3i ToCell(Vector3 local)
    {
        return new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
    }

    private Vector3[] GetDirections(float radius)
    {
        int count = Math.Clamp((int)MathF.Ceiling(4f * MathF.PI * radius * radius * RaysPerSquareUnit), MinRays, MaxRays);
        if (_directionsByCount.TryGetValue(count, out var cached)) return cached;

        var directions = new Vector3[count];
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));

        for (int i = 0; i < count; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / count;
            float ring = MathF.Sqrt(1f - y * y);
            float angle = golden * i;
            directions[i] = new Vector3(MathF.Cos(angle) * ring, y, MathF.Sin(angle) * ring);
        }

        _directionsByCount[count] = directions;
        return directions;
    }
}
