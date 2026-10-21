using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Tools;

public sealed class MultiblockRepair
{
    public struct Result
    {
        public int Completed;
        public int Removed;
        public int OrphanPartsRemoved;
        public List<BrokenMultiblock> Broken;

        public bool Changed => Completed > 0 || Removed > 0 || OrphanPartsRemoved > 0;
    }

    private readonly Func<short, BlockData> _dataOf;
    private readonly Func<short, Block> _create;
    private readonly List<MultiblockCell> _cells = new List<MultiblockCell>();

    public MultiblockRepair(Func<short, BlockData> dataOf, Func<short, Block> create)
    {
        _dataOf = dataOf;
        _create = create;
    }

    public static MultiblockRepair ForCurrentGameSet()
    {
        return new MultiblockRepair(
            id => GameAssets.Blocks.TryGetValue(id, out var data) ? data : null,
            GameAssets.CreateBlockFromId);
    }

    public Result Repair(IReadOnlyDictionary<Vector3SByte, Block[,,]> chunks)
    {
        var result = new Result();

        foreach (var pair in chunks)
        {
            var blocks = pair.Value;
            for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
            for (int z = 0; z < Chunk.Size; z++)
            {
                var block = blocks[x, y, z];
                if (block == null || block.IsAir || block.HasMultiblockOffset) continue;

                var data = _dataOf(block.Id);
                if (data == null || !data.IsMultiblock) continue;

                if (TryComplete(chunks, ToLocal(pair.Key, x, y, z), block, data, out bool filled))
                {
                    if (filled) result.Completed++;
                }
                else
                {
                    result.Broken ??= new List<BrokenMultiblock>();
                    result.Broken.Add(new BrokenMultiblock(ToLocal(pair.Key, x, y, z), block));
                    blocks[x, y, z] = _create(0);
                    result.Removed++;
                }
            }
        }

        foreach (var pair in chunks)
        {
            var blocks = pair.Value;
            for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
            for (int z = 0; z < Chunk.Size; z++)
            {
                var block = blocks[x, y, z];
                if (block == null || block.IsAir || !block.HasMultiblockOffset) continue;
                if (HasOrigin(chunks, ToLocal(pair.Key, x, y, z), block)) continue;

                blocks[x, y, z] = _create(0);
                result.OrphanPartsRemoved++;
            }
        }

        return result;
    }

    private bool TryComplete(IReadOnlyDictionary<Vector3SByte, Block[,,]> chunks, Vector3i origin, Block originBlock, BlockData data, out bool filled)
    {
        filled = false;
        MultiblockLayout.GetCells(data, originBlock.Direction, originBlock.Rotation, _cells);

        for (int i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            if (cell.IsOrigin) continue;

            if (!TryGetCell(chunks, origin + cell.Offset, out var blocks, out var index)) return false;

            var existing = blocks[index.X, index.Y, index.Z];
            if (IsPartOf(existing, originBlock, cell.Model)) continue;
            if (existing != null && !existing.IsAir) return false;
        }

        for (int i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            if (cell.IsOrigin) continue;
            if (!TryGetCell(chunks, origin + cell.Offset, out var blocks, out var index)) continue;
            if (IsPartOf(blocks[index.X, index.Y, index.Z], originBlock, cell.Model)) continue;

            var part = _create(originBlock.Id);
            part.Direction = originBlock.Direction;
            part.Rotation = originBlock.Rotation;
            part.MultiblockOffset = cell.Model;
            part.ConfigureAsMultiblockPart();
            blocks[index.X, index.Y, index.Z] = part;
            filled = true;
        }

        return true;
    }

    private bool HasOrigin(IReadOnlyDictionary<Vector3SByte, Block[,,]> chunks, Vector3i local, Block part)
    {
        var data = _dataOf(part.Id);
        if (data == null || !data.IsMultiblock) return false;

        var model = part.MultiblockOffset;
        if (model.X >= data.Size.X || model.Y >= data.Size.Y || model.Z >= data.Size.Z) return false;

        var origin = local - MultiblockLayout.GetOriginOffset(data, part);
        if (!TryGetCell(chunks, origin, out var blocks, out var index)) return false;

        var originBlock = blocks[index.X, index.Y, index.Z];
        return originBlock != null && originBlock.Id == part.Id && !originBlock.HasMultiblockOffset &&
               originBlock.Direction == part.Direction && originBlock.Rotation == part.Rotation;
    }

    private static bool IsPartOf(Block block, Block origin, Vector3Byte model)
    {
        if (block == null || block.Id != origin.Id) return false;
        if (block.Direction != origin.Direction || block.Rotation != origin.Rotation) return false;

        var offset = block.MultiblockOffset;
        return offset.X == model.X && offset.Y == model.Y && offset.Z == model.Z;
    }

    private static Vector3i ToLocal(Vector3SByte chunk, int x, int y, int z)
    {
        return new Vector3i(chunk.X * Chunk.Size + x, chunk.Y * Chunk.Size + y, chunk.Z * Chunk.Size + z);
    }

    private static bool TryGetCell(IReadOnlyDictionary<Vector3SByte, Block[,,]> chunks, Vector3i local, out Block[,,] blocks, out Vector3i index)
    {
        int cx = (int)MathF.Floor(local.X / (float)Chunk.Size);
        int cy = (int)MathF.Floor(local.Y / (float)Chunk.Size);
        int cz = (int)MathF.Floor(local.Z / (float)Chunk.Size);
        index = new Vector3i(local.X - cx * Chunk.Size, local.Y - cy * Chunk.Size, local.Z - cz * Chunk.Size);

        if (cx < sbyte.MinValue || cx > sbyte.MaxValue || cy < sbyte.MinValue || cy > sbyte.MaxValue || cz < sbyte.MinValue || cz > sbyte.MaxValue)
        {
            blocks = null;
            return false;
        }

        return chunks.TryGetValue(new Vector3SByte((sbyte)cx, (sbyte)cy, (sbyte)cz), out blocks);
    }
}
