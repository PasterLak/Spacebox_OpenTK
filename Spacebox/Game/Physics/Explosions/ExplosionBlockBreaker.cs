using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Doors;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Physics.Explosions;

public sealed class ExplosionBlockBreaker
{
    private const int MaxDestructionEffects = 48;
    private const float DropSpeed = 6f;
    private const float DropPickupDelay = 0.5f;
    private const float DropDeceleration = 4f;

    private readonly HashSet<Chunk> _touchedChunks = new HashSet<Chunk>();
    private readonly List<Vector3i> _specialCells = new List<Vector3i>();
    private readonly Dictionary<(short ItemId, Chunk Chunk), DropGroup> _drops = new Dictionary<(short, Chunk), DropGroup>();
    private readonly Random _random = new Random();

    public void Break(SpaceEntity entity, HashSet<Vector3i> cells, Vector3i origin, Vector3 worldCenter, ExplosiveBlockData data, Action<Chunk, Vector3Byte, ExplosiveBlockData> chainExplosive)
    {
        _touchedChunks.Clear();
        _specialCells.Clear();
        _drops.Clear();

        int effects = 0;

        foreach (var cell in cells)
        {
            if (!entity.TryGetBlockLocal(cell, out var chunk, out var index)) continue;

            var block = chunk.Blocks[index.X, index.Y, index.Z];
            if (block == null || block.IsAir) continue;

            if (cell == origin)
            {
                if (chunk.ClearCellForBulkEdit(index.X, index.Y, index.Z)) _touchedChunks.Add(chunk);
                continue;
            }

            if (block is ExplosiveBlock explosive)
            {
                chainExplosive(chunk, index, explosive.Data);
                continue;
            }

            if (NeedsFullRemoval(block))
            {
                _specialCells.Add(cell);
                continue;
            }

            if (data.DropBlocks) CollectDrop(entity, chunk, cell, block);

            if (effects < MaxDestructionEffects)
            {
                var effectPosition = entity.BlockCenterToWorldCorner(chunk.PositionWorld + new Vector3(index.X, index.Y, index.Z));
                World.DestructionManager?.DestroyBlock(effectPosition, block.LightColor, block);
                effects++;
            }

            if (chunk.ClearCellForBulkEdit(index.X, index.Y, index.Z)) _touchedChunks.Add(chunk);
        }

        RemoveSpecialBlocks(entity, data);

        foreach (var chunk in _touchedChunks)
            chunk.FinishBulkEdit();

        if (_touchedChunks.Count > 0) entity.Oxygen.OnChunksChanged();

        SpawnDrops(entity, worldCenter);
    }

    private static bool NeedsFullRemoval(Block block)
    {
        if (block.OnDestroy != null || block is ElectricalBlock || block is IEntityBlockListener || block is DoorBlock)
            return true;

        return GameAssets.Blocks.TryGetValue(block.Id, out var data) && data.IsMultiblock;
    }

    private void RemoveSpecialBlocks(SpaceEntity entity, ExplosiveBlockData data)
    {
        for (int i = 0; i < _specialCells.Count; i++)
        {
            var cell = _specialCells[i];
            if (!entity.TryGetBlockLocal(cell, out var chunk, out var index)) continue;

            var block = chunk.Blocks[index.X, index.Y, index.Z];
            if (block == null || block.IsAir) continue;

            var hit = new HitInfo
            {
                chunk = chunk,
                block = block,
                blockPositionIndex = index,
                blockPositionEntity = cell,
                blockPositionWorld = entity.EntityLocalToWorld(new Vector3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z + 0.5f))
            };

            if (!data.DestroyStorageItems) block.OnDestroy?.Invoke(hit);
            chunk.RemoveBlock(index.X, index.Y, index.Z, 0, 0, 0, data.DropBlocks);
        }
    }

    private void CollectDrop(SpaceEntity entity, Chunk chunk, Vector3i cell, Block block)
    {
        var data = GameAssets.GetBlockDataById(block.Id);
        if (data == null || data.Drop == null || data.Drop.Item == null || data.Drop.Count <= 0) return;

        var key = (data.Drop.Item.Id, chunk);
        _drops.TryGetValue(key, out var group);
        group.Item = data.Drop.Item;
        group.Count += data.Drop.Count;
        group.PositionSum += new Vector3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z + 0.5f);
        group.Cells++;
        _drops[key] = group;
    }

    private void SpawnDrops(SpaceEntity entity, Vector3 worldCenter)
    {
        var dropManager = World.DropEffectManager;
        if (dropManager == null) return;

        foreach (var group in _drops.Values)
        {
            var position = entity.EntityLocalToWorld(group.PositionSum / group.Cells);
            int stackSize = Math.Max(1, (int)group.Item.StackSize);
            int remaining = group.Count;

            while (remaining > 0)
            {
                int count = Math.Min(stackSize, remaining);
                remaining -= count;
                dropManager.DropItem(position, GetDropDirection(position, worldCenter), DropSpeed, group.Item, count, DropPickupDelay, DropDeceleration, entity);
            }
        }
    }

    private Vector3 GetDropDirection(Vector3 position, Vector3 worldCenter)
    {
        var jitter = new Vector3(
            (float)_random.NextDouble() - 0.5f,
            (float)_random.NextDouble() - 0.5f,
            (float)_random.NextDouble() - 0.5f);

        var direction = position - worldCenter;
        direction = direction.LengthSquared > 1e-4f ? direction.Normalized() : jitter;
        return (direction + jitter * 0.6f).Normalized();
    }

    private struct DropGroup
    {
        public Item Item;
        public int Count;
        public Vector3 PositionSum;
        public int Cells;
    }
}
