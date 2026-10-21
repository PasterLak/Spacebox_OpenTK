using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Physics;
using Spacebox.Game.Resource;
using Spacebox.Network;

namespace Spacebox.Game.Generation.Tools;

public static class BrokenMultiblockSpill
{
    private const int MaxWaitTicks = 600;

    public static void Schedule(SpaceEntity entity, IReadOnlyList<BrokenMultiblock> broken)
    {
        if (entity == null || broken == null || broken.Count == 0) return;

        for (int i = 0; i < broken.Count; i++)
        {
            var item = broken[i];
            Debug.Warning($"[MultiblockRepair] Entity {entity.EntityID}: multi-cell block '{GameAssets.GetBlockFullId(item.Block)}' at cell {item.Cell} could not be completed and was removed; its contents and block item are dropped");
        }

        if (GameSession.IsRemoteClient) return;

        var pending = new List<BrokenMultiblock>(broken);
        var task = new TickTask(MaxWaitTicks, false);
        task.OnTick = () =>
        {
            if (TrySpill(entity, pending)) task.Stop();
        };
        task.OnComplete = () =>
        {
            if (!task.IsStopped) Debug.Warning($"[MultiblockRepair] Entity {entity.EntityID} was not loaded in time; {pending.Count} removed multi-cell blocks were not dropped");
        };
        TickTaskManager.AddTask(task);
    }

    private static bool TrySpill(SpaceEntity entity, List<BrokenMultiblock> broken)
    {
        var world = World.Instance;
        var drops = World.DropEffectManager;
        if (world == null || drops == null) return false;
        if (!world.TryFindEntity(entity.EntityID, out var loaded) || loaded != entity) return false;

        for (int i = 0; i < broken.Count; i++)
        {
            var cell = broken[i].Cell;
            var block = broken[i].Block;
            var center = entity.EntityLocalToWorld(new Vector3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z + 0.5f));

            var hit = new HitInfo
            {
                block = block,
                blockPositionEntity = cell,
                blockPositionWorld = center
            };

            block.OnDestroy?.Invoke(hit);
            drops.DropBlock(center - new Vector3(0.5f), block.LightColor, block, entity);
        }

        return true;
    }
}
