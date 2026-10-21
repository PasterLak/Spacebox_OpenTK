
using Engine;
using Engine.Animation;
using Engine.Audio;
using Engine.Physics;

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.GUI;

namespace Spacebox.Game.Player.Interactions;

public class InteractionDestroyBlockCreative : InteractionDestroyBlock
{
    private const float MinBlockDestroyTime = 0.175f;
    private readonly RepeatingAction _destroyAction = new RepeatingAction("block_destroy", MinBlockDestroyTime);

    public InteractionDestroyBlockCreative(ItemSlot itemSlot) : base(itemSlot)
    {
    }

    public override void OnEnable()
    {
        if (BlockSelector.Instance != null)
            BlockSelector.Instance.SimpleBlock.Material.Shader.SetVector4("color", Vector4.One);
        BlockSelector.IsVisible = false;
        model.Animator.AddAnimation(new MoveAnimation(model.Position, model.Position + new Vector3(0.005f, 0, 0), 0.05f, true));
    }

    public override void Update(LocalAstronaut player)
    {
        if (Input.IsActionUp("block_destroy"))
        {
            model.SetAnimation(false);
        }
        if (!player.CanMove)
        {
            model.SetAnimation(false);
            return;
        }
        BlockUseTarget.Update(player);
        Ray ray = new Ray(player.Position, player.Front, 10);
        bool hasVoxelHit = World.CurrentSector.Raycast(ray, out HitInfo hit);

        if (TryDrillTarget(ray, player, hasVoxelHit, hit))
        {
            if (Input.IsActionDown("block_destroy"))
                model.SetAnimation(true);
            return;
        }

        if (hasVoxelHit)
        {
            UpdateBlockSelector(hit);
            if (Input.IsActionDown("block_destroy"))
                model.SetAnimation(true);
            if (_destroyAction.Poll())
                BlockDestroyer.DestroyInstantly(hit, player);
            //else CenteredText.Hide();
        }
        else
        {
            AImedBlockElement.AimedBlock = null;
            BlockSelector.IsVisible = false;
            CenteredText.Hide();
            if (Input.IsActionDown("block_destroy"))
                model.SetAnimation(true);
        }
    }

    public override void Render(LocalAstronaut player)
    {
    }
}
