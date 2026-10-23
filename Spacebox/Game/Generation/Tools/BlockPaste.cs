using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Player.Building;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Tools;

public static class BlockPaste
{
    public static BlockClipboard Capture(SpaceEntity entity, Vector3i min, Vector3i max, out string error)
    {
        error = null;
        BulkBlockEdit.Normalize(ref min, ref max);
        var size = max - min + Vector3i.One;
        if (entity == null || !BlockClipboard.IsValidSize(size))
        {
            error = $"The area can be at most {BlockClipboard.MaxSize} blocks on each side";
            return null;
        }

        var cells = new List<ClipboardCell>();
        for (int x = min.X; x <= max.X; x++)
            for (int y = min.Y; y <= max.Y; y++)
                for (int z = min.Z; z <= max.Z; z++)
                {
                    if (!entity.TryGetBlockLocal(new Vector3i(x, y, z), out var chunk, out var index)) continue;
                    var block = chunk.Blocks[index.X, index.Y, index.Z];
                    if (block == null || block.IsAir || block.HasMultiblockOffset) continue;

                    if (cells.Count >= BlockClipboard.MaxCells)
                    {
                        error = $"Too many blocks to copy (at most {BlockClipboard.MaxCells})";
                        return null;
                    }
                    cells.Add(new ClipboardCell(new Vector3i(x - min.X, y - min.Y, z - min.Z), block.Id, block.Direction, block.Rotation));
                }

        if (cells.Count == 0)
        {
            error = "There are no blocks in the selected area";
            return null;
        }
        return new BlockClipboard(size, cells);
    }

    public static bool TryValidate(Vector3i min, Vector3i size, IReadOnlyList<ClipboardCell> cells, out string error)
    {
        error = null;
        if (cells == null || cells.Count == 0 || cells.Count > BlockClipboard.MaxCells || !BlockClipboard.IsValidSize(size))
        {
            error = "Invalid paste size";
            return false;
        }
        if (!BulkBlockEdit.TryValidate(min, min + size - Vector3i.One, BulkBlockEdit.EraseBlockId, out error)) return false;

        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            if (cell.Offset.X < 0 || cell.Offset.Y < 0 || cell.Offset.Z < 0 || cell.Offset.X >= size.X || cell.Offset.Y >= size.Y || cell.Offset.Z >= size.Z ||
                (byte)cell.Direction > 5 || (byte)cell.Rotation > 3 || cell.BlockId == 0 || !GameAssets.Blocks.ContainsKey(cell.BlockId))
            {
                error = "Invalid block in paste";
                return false;
            }
        }
        return true;
    }

    public static void Apply(SpaceEntity entity, Vector3i min, IReadOnlyList<ClipboardCell> cells)
    {
        if (entity == null || cells == null) return;

        var modified = new HashSet<Chunk>();
        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            var target = min + cell.Offset;
            if (entity.TryGetBlockLocal(target, out var existing, out var existingIndex))
            {
                var occupant = existing.Blocks[existingIndex.X, existingIndex.Y, existingIndex.Z];
                if (occupant != null && !occupant.IsAir) continue;
            }

            var data = GameAssets.GetBlockDataById(cell.BlockId);
            if (data == null) continue;

            var block = GameAssets.CreateBlockFromId(cell.BlockId);
            block.Direction = cell.Direction;
            block.Rotation = cell.Rotation;

            if (data.IsMultiblock)
            {
                entity.TryPlaceBlockAtCell(target, block);
                continue;
            }

            var chunkIndex = new Vector3SByte((sbyte)FloorDiv(target.X), (sbyte)FloorDiv(target.Y), (sbyte)FloorDiv(target.Z));
            var chunk = SpaceEntity.GetOrCreateChunk(entity, chunkIndex);
            if (chunk == null) continue;

            chunk.SetCellForBulkEdit((byte)(target.X - chunkIndex.X * Chunk.Size), (byte)(target.Y - chunkIndex.Y * Chunk.Size), (byte)(target.Z - chunkIndex.Z * Chunk.Size), block);
            modified.Add(chunk);
        }

        foreach (var chunk in modified)
        {
            chunk.IsModified = true;
            chunk.FinishBulkEdit();
        }
        if (modified.Count > 0) entity.Oxygen.OnChunksChanged();
    }

    private static int FloorDiv(int value) => (int)MathF.Floor((float)value / Chunk.Size);
}
