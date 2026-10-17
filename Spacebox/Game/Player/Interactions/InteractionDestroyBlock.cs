namespace Spacebox.Game.Player.Interactions;

using Engine;
using Engine.Audio;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.GUI;
using System;

public abstract class InteractionDestroyBlock : InteractionMode
{
    protected ItemSlot selectedItemSlot;
    protected DrillItem drillItem;
    protected AnimatedItemModel model;

    public override bool UsesPrimaryAction => true;

    protected InteractionDestroyBlock(ItemSlot itemSlot)
    {
        AllowReload = true;
        selectedItemSlot = itemSlot;
        drillItem = itemSlot.Item as DrillItem;
        model = GameAssets.ItemModels[itemSlot.Item.Id] as AnimatedItemModel;
    }

    protected void DestroyBlock(HitInfo hit, LocalAstronaut player)
    {
        BlockDestroyer.Destroy(hit, player);
    }

    protected bool TryDrillTarget(Engine.Physics.Ray ray, LocalAstronaut player, bool hasVoxelHit, in HitInfo voxelHit)
    {
        var world = World.Instance;
        if (world == null || drillItem == null) return false;
        if (!world.Physics.Raycast(ray, player.Body, out var hit)) return false;
        if (hit.Body.Owner is not IDrillable target) return false;
        if (hasVoxelHit && Vector3.DistanceSquared(ray.Origin, voxelHit.hitPosition) < hit.Distance * hit.Distance) return false;

        BlockSelector.IsVisible = false;
        AImedBlockElement.AimedBlock = null;

        if (Input.IsAction("block_destroy"))
            target.TakeDrillDamage(drillItem.Tier * Time.Delta);

        return true;
    }

    protected void UpdateBlockSelector(HitInfo hit)
    {
        BlockSelector.Instance.Enabled = true;
        AImedBlockElement.AimedBlock = hit.block;
        var entity = hit.chunk.SpaceEntity;
        Vector3 selectorPos = hit.blockPositionIndex + hit.chunk.PositionWorld;
        Vector3 highlightSize = Vector3.One;

        if (entity.TryGetStructureBounds(hit.chunk, hit.blockPositionIndex, out var structureMin, out var structureSize))
        {
            highlightSize = structureSize;
            selectorPos = structureMin + structureSize * 0.5f - new Vector3(0.5f);
        }

        BlockSelector.Instance.SetHighlightSize(highlightSize);
        BlockSelector.Instance.UpdatePosition(selectorPos, Block.GetDirectionFromNormal(hit.normal), Rotation.None, false, entity);
        BlockSelector.Instance.LineRenderer.Enabled = false;
    }

    public override void OnDisable()
    {
        BlockSelector.IsVisible = false;
        selectedItemSlot = null;
        model?.Animator.Clear();
    }
}