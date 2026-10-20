using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Player.Interactions;

public sealed class RepairTarget
{
    private readonly List<Vector3i> _cells = new List<Vector3i>();

    public DurabilityTally Tally { get; private set; } = DurabilityTally.Empty;
    public Chunk WeakestChunk { get; private set; }
    public Vector3Byte WeakestIndex { get; private set; }
    public Block WeakestBlock { get; private set; }

    public void Measure(Chunk chunk, Vector3Byte index, Block block, byte max)
    {
        var tally = DurabilityTally.Empty;
        WeakestChunk = chunk;
        WeakestIndex = index;
        WeakestBlock = block;

        var entity = chunk.SpaceEntity;
        if (entity == null)
        {
            tally.Add(0, block.Durability, max);
            Tally = tally;
            return;
        }

        entity.GetStructureCells(chunk, index, _cells);
        for (int i = 0; i < _cells.Count; i++)
        {
            if (!entity.TryGetBlockLocal(_cells[i], out var cellChunk, out var cellIndex)) continue;

            var cell = cellChunk.Blocks[cellIndex.X, cellIndex.Y, cellIndex.Z];
            if (cell == null || cell.IsAir || cell.Id != block.Id) continue;

            int before = tally.WeakestIndex;
            tally.Add(i, cell.Durability, max);
            if (tally.WeakestIndex != before)
            {
                WeakestChunk = cellChunk;
                WeakestIndex = cellIndex;
                WeakestBlock = cell;
            }
        }

        if (tally.WeakestIndex < 0) tally.Add(0, block.Durability, max);
        Tally = tally;
    }
}
