using Engine;
using Engine.AI;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Mobs.Navigation;

namespace Spacebox.Game.Mobs.Feeding;

public readonly struct FoodTarget
{
    public readonly SpaceEntity? Entity;
    public readonly Vector3i Cell;
    public readonly Vector3i FeedCell;

    public FoodTarget(SpaceEntity entity, Vector3i cell, Vector3i feedCell)
    {
        Entity = entity;
        Cell = cell;
        FeedCell = feedCell;
    }

    public bool IsValid => Entity != null;
    public Vector3 BlockWorld => Entity!.EntityLocalToWorld(VoxelLine.CellCenter(Cell));
    public Vector3 FeedWorld => Entity!.EntityLocalToWorld(VoxelLine.CellCenter(FeedCell));
    public Vector3i Normal => FeedCell - Cell;

    public bool IsEdible(MobNavigationGrid grid, MobDefinition definition)
    {
        if (Entity == null || grid.Anchor != Entity) return false;
        return grid.TryGetBlock(Cell, out _, out _, out var block) && !block.IsAir && definition.CanEat(block.Id);
    }
}
