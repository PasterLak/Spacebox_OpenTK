namespace Spacebox.Game.Player.Interactions;

using Engine;
using Engine.Audio;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;
using Spacebox.Game.Player.Building;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.GUI;
using System;

public abstract class InteractionDestroyBlock : InteractionMode
{
    private const float ModelHighlightPadding = 0.04f;
    private static readonly List<MultiblockCell> _boundsCells = new List<MultiblockCell>();
    protected ItemSlot selectedItemSlot;
    protected DrillItem drillItem;
    protected AnimatedItemModel model;
    private bool _targetEffectActive;

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
        if (world == null || drillItem == null || !TryFindDrillTarget(world, ray, player, hasVoxelHit, voxelHit, out var target, out var hitPoint))
        {
            HideTargetHitEffect(world);
            return false;
        }

        BlockSelector.IsVisible = false;
        AImedBlockElement.AimedBlock = null;

        if (Input.IsAction("block_destroy"))
        {
            float damage = drillItem.Damage * Time.Delta;
            target.TakeDrillDamage(damage);
            player.PlayerStatistics.RecordEntityDamage(damage);
            ShowTargetHitEffect(world, player, hitPoint);
        }
        else
        {
            HideTargetHitEffect(world);
        }

        return true;
    }

    private static bool TryFindDrillTarget(World world, Engine.Physics.Ray ray, LocalAstronaut player, bool hasVoxelHit, in HitInfo voxelHit, out IDrillable target, out Vector3 hitPoint)
    {
        target = null;
        hitPoint = default;

        if (!world.Physics.Raycast(ray, player.Body, out var hit)) return false;
        if (hit.Body.Owner is not IDrillable drillable) return false;
        if (hasVoxelHit && Vector3.DistanceSquared(ray.Origin, voxelHit.hitPosition) < hit.Distance * hit.Distance) return false;

        target = drillable;
        hitPoint = hit.Point;
        return true;
    }

    private void ShowTargetHitEffect(World world, LocalAstronaut player, Vector3 hitPoint)
    {
        var effect = world.BlockMiningEffect;
        if (effect == null) return;

        _targetEffectActive = true;
        effect.Enabled = true;
        effect.SetEmitter(true);
        effect.ParticleSystem.Position = hitPoint;
        if (effect.ParticleSystem.Emitter is ConeEmitter cone)
            cone.Direction = Vector3.Normalize(player.Position - hitPoint);
    }

    private void HideTargetHitEffect(World world)
    {
        if (!_targetEffectActive) return;
        _targetEffectActive = false;

        var effect = world?.BlockMiningEffect;
        if (effect == null) return;

        effect.Enabled = false;
        effect.ClearParticles();
    }

    protected void UpdateBlockSelector(HitInfo hit)
    {
        BlockSelector.Instance.Enabled = true;
        AImedBlockElement.AimedBlock = hit.block;
        var entity = hit.chunk.SpaceEntity;
        Vector3 selectorPos = hit.blockPositionIndex + hit.chunk.PositionWorld;
        Vector3 highlightSize = Vector3.One;

        if (TryGetModelBounds(hit, entity, out var boundsMin, out var boundsMax))
        {
            highlightSize = boundsMax - boundsMin + new Vector3(ModelHighlightPadding);
            selectorPos = (boundsMin + boundsMax) * 0.5f - new Vector3(0.5f);
        }
        else if (entity.TryGetStructureBounds(hit.chunk, hit.blockPositionIndex, out var structureMin, out var structureSize))
        {
            highlightSize = structureSize;
            selectorPos = structureMin + structureSize * 0.5f - new Vector3(0.5f);
        }

        BlockSelector.Instance.SetHighlightSize(highlightSize);
        BlockSelector.Instance.UpdatePosition(selectorPos, Block.GetDirectionFromNormal(hit.normal), Rotation.None, false, entity);
        BlockSelector.Instance.LineRenderer.Enabled = false;
    }

    private static bool TryGetModelBounds(HitInfo hit, SpaceEntity entity, out Vector3 min, out Vector3 max)
    {
        min = Vector3.Zero;
        max = Vector3.Zero;
        var block = hit.block;
        var data = block == null ? null : GameAssets.GetBlockDataById(block.Id);
        if (data?.Shape == null) return false;

        var origin = hit.blockPositionEntity;
        if (block.HasMultiblockOffset) origin -= MultiblockLayout.GetOriginOffset(data, block);
        bool supported = BlockShapes.IsSupported(entity, origin, data.Shape, block.Direction, block.Rotation);
        PlacementBounds.Get(data, block.Direction, block.Rotation, supported, _boundsCells, out min, out max);

        var corner = entity.CellCorner(origin);
        min += corner;
        max += corner;
        return true;
    }

    public override void OnDisable()
    {
        BlockSelector.IsVisible = false;
        selectedItemSlot = null;
        model?.Animator.Clear();
        HideTargetHitEffect(World.Instance);
    }
}