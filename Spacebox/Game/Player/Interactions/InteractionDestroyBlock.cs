namespace Spacebox.Game.Player.Interactions;

using Client;
using Engine;
using Engine.Audio;
using global::Client;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.GUI;
using SpaceNetwork;
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

    protected void UpdateBlockSelector(HitInfo hit)
    {
        BlockSelector.Instance.Enabled = true;
        AImedBlockElement.AimedBlock = hit.block;
        Vector3 selectorPos = hit.blockPositionIndex + hit.chunk.PositionWorld;
        BlockSelector.Instance.UpdatePosition(selectorPos, Block.GetDirectionFromNormal(hit.normal), Rotation.None, false);
        BlockSelector.Instance.LineRenderer.Enabled = false;
    }

    public override void OnDisable()
    {
        BlockSelector.IsVisible = false;
        selectedItemSlot = null;
        model?.Animator.Clear();
    }
}