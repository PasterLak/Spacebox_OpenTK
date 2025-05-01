namespace Spacebox.Game.Player;
using Engine;
using Engine.Animation;
using Engine.Audio;
using Engine.Physics;

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.GUI;


public class InteractionDestroyBlockCreative : InteractionDestroyBlock
{
    private const float MinBlockDestroyTime = 0.175f;
    private float _time = MinBlockDestroyTime;
    private InteractiveBlock lastInteractiveBlock;

    public InteractionDestroyBlockCreative(ItemSlot itemSlot) : base(itemSlot)
    {
    }

    public override void OnEnable()
    {
        if (blockDestroy == null)
        {
            blockDestroy = new AudioSource(SoundManager.GetClip("blockDestroyDefault"));
            blockDestroy.Volume = 1f;
        }
        if (BlockSelector.Instance != null)
            BlockSelector.Instance.SimpleBlock.Material.Shader.SetVector4("color", Vector4.One);
        BlockSelector.IsVisible = false;
        model.Animator.AddAnimation(new MoveAnimation(model.Position, model.Position + new Vector3(0.005f, 0, 0), 0.05f, true));
    }

    public override void Update(Astronaut player)
    {
        if (Input.IsMouseButtonUp(MouseButton.Left))
        {
            _time = MinBlockDestroyTime;
            model.SetAnimation(false);
        }
        if (!player.CanMove)
        {
            model.SetAnimation(false);
            return;
        }
        Ray ray = new Ray(player.Position, player.Front, MaxDestroyDistance);
        if (World.CurrentSector.Raycast(ray, out HitInfo hit))
        {
            UpdateBlockSelector(hit);
            if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                _time = MinBlockDestroyTime;
                model.SetAnimation(true);
                DestroyBlock(hit);
            }
            if (Input.IsMouseButton(MouseButton.Left))
            {
                _time -= Time.Delta;
                if (_time <= 0)
                {
                    _time = MinBlockDestroyTime;
                    DestroyBlock(hit);
                }
            }
            if (hit.block.Is<InteractiveBlock>(out var b))
            {
                lastInteractiveBlock = b;
                InteractiveBlock.UpdateInteractive(lastInteractiveBlock, player, hit.chunk, hit.position);

                if (hit.block.Is<StorageBlock>(out var storageBlock))
                {

                    storageBlock.SetPositionInEntity((Vector3i)(hit.blockPositionEntity));
                }
            }
            else CenteredText.Hide();
        }
        else
        {
            AImedBlockElement.AimedBlock = null;
            BlockSelector.IsVisible = false;
            CenteredText.Hide();
            if (Input.IsMouseButtonDown(MouseButton.Left))
                model.SetAnimation(true);
        }
    }

    public override void Render(Astronaut player)
    {
    }
}
