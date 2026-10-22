using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipBlockGrid
{
    private const int Size = Chunk.Size;

    private readonly IReadOnlyDictionary<Vector3SByte, Block[,,]> _chunks;
    private readonly Func<Block> _createAir;
    private readonly List<MultiblockCell> _structureCells = new List<MultiblockCell>();

    public ShipBlockGrid(IReadOnlyDictionary<Vector3SByte, Block[,,]> chunks, Func<Block> createAir)
    {
        _chunks = chunks;
        _createAir = createAir;
    }

    public Block Get(Vector3i cell)
    {
        if (!TryChunkOf(cell, out var chunk) || !_chunks.TryGetValue(chunk, out var blocks)) return null;
        return blocks[Local(cell.X), Local(cell.Y), Local(cell.Z)];
    }

    public bool IsSolid(Vector3i cell)
    {
        var block = Get(cell);
        return block != null && !block.IsAir;
    }

    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out Vector3i cell, out Vector3i normal, out float distance)
    {
        cell = new Vector3i((int)MathF.Floor(origin.X), (int)MathF.Floor(origin.Y), (int)MathF.Floor(origin.Z));
        normal = Vector3i.Zero;
        distance = 0f;

        var step = new Vector3i(MathF.Sign(direction.X), MathF.Sign(direction.Y), MathF.Sign(direction.Z));
        var next = new Vector3(Boundary(origin.X, cell.X, step.X, direction.X), Boundary(origin.Y, cell.Y, step.Y, direction.Y), Boundary(origin.Z, cell.Z, step.Z, direction.Z));
        var delta = new Vector3(Delta(direction.X), Delta(direction.Y), Delta(direction.Z));

        while (distance <= maxDistance)
        {
            if (IsSolid(cell)) return true;

            if (next.X < next.Y && next.X < next.Z)
            {
                cell.X += step.X;
                distance = next.X;
                next.X += delta.X;
                normal = new Vector3i(-step.X, 0, 0);
            }
            else if (next.Y < next.Z)
            {
                cell.Y += step.Y;
                distance = next.Y;
                next.Y += delta.Y;
                normal = new Vector3i(0, -step.Y, 0);
            }
            else
            {
                cell.Z += step.Z;
                distance = next.Z;
                next.Z += delta.Z;
                normal = new Vector3i(0, 0, -step.Z);
            }
        }

        return false;
    }

    public bool Damage(Vector3i cell, int damage, List<Vector3i> changed)
    {
        var block = Get(cell);
        if (damage <= 0 || block == null || block.IsAir || UnbreakableBlocks.Is(block)) return false;

        if (block.Durability - damage > 0)
        {
            block.Durability = (byte)(block.Durability - damage);
            changed.Add(cell);
            return false;
        }

        Remove(cell, block, changed);
        return true;
    }

    public void CollectAffectedChunks(List<Vector3i> cells, HashSet<Vector3SByte> chunks)
    {
        for (int i = 0; i < cells.Count; i++)
        for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        for (int z = -1; z <= 1; z++)
        {
            if (TryChunkOf(cells[i] + new Vector3i(x, y, z), out var chunk) && _chunks.ContainsKey(chunk)) chunks.Add(chunk);
        }
    }

    private void Remove(Vector3i cell, Block block, List<Vector3i> changed)
    {
        if (GameAssets.Blocks.TryGetValue(block.Id, out var data) && data.IsMultiblock)
        {
            var origin = cell - MultiblockLayout.GetOriginOffset(data, block);
            MultiblockLayout.GetCells(data, block.Direction, block.Rotation, _structureCells);
            for (int i = 0; i < _structureCells.Count; i++)
            {
                var structureCell = origin + _structureCells[i].Offset;
                var part = Get(structureCell);
                var model = _structureCells[i].Model;
                if (part == null || part.Id != block.Id || part.Direction != block.Direction || part.Rotation != block.Rotation ||
                    part.MultiblockOffset.X != model.X || part.MultiblockOffset.Y != model.Y || part.MultiblockOffset.Z != model.Z) continue;
                SetAir(structureCell, changed);
            }
        }

        if (IsSolid(cell)) SetAir(cell, changed);
    }

    private void SetAir(Vector3i cell, List<Vector3i> changed)
    {
        if (!TryChunkOf(cell, out var chunk) || !_chunks.TryGetValue(chunk, out var blocks)) return;
        blocks[Local(cell.X), Local(cell.Y), Local(cell.Z)] = _createAir();
        changed.Add(cell);
    }

    private static bool TryChunkOf(Vector3i cell, out Vector3SByte chunk)
    {
        int x = FloorDiv(cell.X), y = FloorDiv(cell.Y), z = FloorDiv(cell.Z);
        if (x < sbyte.MinValue || x > sbyte.MaxValue || y < sbyte.MinValue || y > sbyte.MaxValue || z < sbyte.MinValue || z > sbyte.MaxValue)
        {
            chunk = default;
            return false;
        }

        chunk = new Vector3SByte((sbyte)x, (sbyte)y, (sbyte)z);
        return true;
    }

    private static int FloorDiv(int value) => value >= 0 ? value / Size : (value - Size + 1) / Size;

    private static int Local(int value) => value - FloorDiv(value) * Size;

    private static float Boundary(float origin, int cell, int step, float direction)
    {
        if (step == 0) return float.PositiveInfinity;
        return (step > 0 ? cell + 1 - origin : origin - cell) / MathF.Abs(direction);
    }

    private static float Delta(float direction) => direction == 0f ? float.PositiveInfinity : 1f / MathF.Abs(direction);
}
