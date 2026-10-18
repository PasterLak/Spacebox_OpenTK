using Engine;
using Engine.AI;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Mobs.Navigation;

public sealed class MobNavigationGrid : INavigationGrid
{
    private Vector3 _origin;
    private Chunk? _lastChunk;
    private Vector3i _lastChunkIndex;

    public SpaceEntity? Anchor { get; private set; }

    public void Bind(Vector3 worldPosition)
    {
        _lastChunk = null;
        Anchor = FindEntity(worldPosition);
        var cell = VoxelLine.ToCell(worldPosition);
        _origin = Anchor == null ? new Vector3(cell.X, cell.Y, cell.Z) : Vector3.Zero;
    }

    public void Bind(SpaceEntity entity)
    {
        _lastChunk = null;
        Anchor = entity;
        _origin = Vector3.Zero;
    }

    public Vector3 ToGrid(Vector3 worldPosition)
    {
        return Anchor != null ? Anchor.WorldToEntityLocal(worldPosition) : worldPosition - _origin;
    }

    public Vector3 ToWorld(Vector3 gridPosition)
    {
        return Anchor != null ? Anchor.EntityLocalToWorld(gridPosition) : gridPosition + _origin;
    }

    public Vector3 CellToWorld(Vector3i cell)
    {
        return ToWorld(VoxelLine.CellCenter(cell));
    }

    public Vector3i WorldToCell(Vector3 worldPosition)
    {
        return VoxelLine.ToCell(ToGrid(worldPosition));
    }

    public bool IsPassable(Vector3i cell)
    {
        var block = GetBlock(cell);
        return block == null || block.IsAir;
    }

    public bool IsSolid(Vector3i cell)
    {
        return !IsPassable(cell);
    }

    public float GetLightLevel(Vector3i cell)
    {
        return GetBlock(cell)?.LightLevel ?? 0f;
    }

    public Block? GetBlock(Vector3i cell)
    {
        return TryGetBlock(cell, out _, out _, out var block) ? block : null;
    }

    public bool TryGetBlock(Vector3i cell, out Chunk chunk, out Vector3Byte index, out Block block)
    {
        chunk = null!;
        index = default;
        block = null!;
        if (Anchor == null) return false;

        var chunkIndex = new Vector3i(FloorDiv(cell.X), FloorDiv(cell.Y), FloorDiv(cell.Z));
        if (!TryGetChunk(chunkIndex, out chunk)) return false;

        index = new Vector3Byte(
            (byte)(cell.X - chunkIndex.X * Chunk.Size),
            (byte)(cell.Y - chunkIndex.Y * Chunk.Size),
            (byte)(cell.Z - chunkIndex.Z * Chunk.Size));

        block = chunk.Blocks[index.X, index.Y, index.Z];
        return block != null;
    }

    public bool HasLineOfSight(Vector3 fromWorld, Vector3 toWorld)
    {
        return VoxelLine.IsClear(this, ToGrid(fromWorld), ToGrid(toWorld));
    }

    private bool TryGetChunk(Vector3i chunkIndex, out Chunk chunk)
    {
        if (_lastChunk != null && _lastChunkIndex == chunkIndex && !_lastChunk.IsDisposed)
        {
            chunk = _lastChunk;
            return true;
        }

        chunk = null!;
        if (chunkIndex.X < sbyte.MinValue || chunkIndex.X > sbyte.MaxValue ||
            chunkIndex.Y < sbyte.MinValue || chunkIndex.Y > sbyte.MaxValue ||
            chunkIndex.Z < sbyte.MinValue || chunkIndex.Z > sbyte.MaxValue) return false;

        if (!Anchor!.TryGetChunk(new Vector3SByte((sbyte)chunkIndex.X, (sbyte)chunkIndex.Y, (sbyte)chunkIndex.Z), out chunk) || chunk.IsDisposed)
            return false;

        _lastChunk = chunk;
        _lastChunkIndex = chunkIndex;
        return true;
    }

    private static int FloorDiv(int value)
    {
        return value >= 0 ? value / Chunk.Size : (value - Chunk.Size + 1) / Chunk.Size;
    }

    private static SpaceEntity? FindEntity(Vector3 worldPosition)
    {
        var sector = World.SectorAt(worldPosition);
        if (sector == null || !sector.TryGetNearestEntity(worldPosition, out var entity) || entity == null) return null;

        return entity.IsPositionWithinEntitySize(entity.ToEntitySpace(worldPosition)) ? entity : null;
    }
}
