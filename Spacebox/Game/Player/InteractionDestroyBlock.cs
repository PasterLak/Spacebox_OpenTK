using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Common.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.GUI;

namespace Spacebox.Game.Player;

public class InteractionDestroyBlock : InteractionMode
{
    private const byte MaxDestroyDistance = 6;

    private static AudioSource blockDestroy;
    private InteractiveBlock lastInteractiveBlock;
    private ItemSlot selectedItemSlot;
    private AnimatedItemModel model;

    public InteractionDestroyBlock(ItemSlot itemslot)
    {
        AllowReload = true;
        selectedItemSlot = itemslot;

        UpdateItemSlot(itemslot);
    }
    public void UpdateItemSlot(ItemSlot itemslot)
    {
        selectedItemSlot = itemslot;
        var mod = GameBlocks.ItemModels[itemslot.Item.Id];
        model = mod as AnimatedItemModel;

    }

    public override void OnEnable()
    {
        if (blockDestroy == null)
        {
            blockDestroy = new AudioSource(SoundManager.GetClip("blockDestroyDefault"));
            blockDestroy.Volume = 1f;
        }

        if (BlockSelector.Instance != null)
            BlockSelector.Instance.SimpleBlock.Shader.SetVector4("color", Vector4.One);
        BlockSelector.IsVisible = false;


    }

    public override void OnDisable()
    {
        BlockSelector.IsVisible = false;
        CenteredText.Hide();

        selectedItemSlot = null;
    }

    private void PickDestroySound(short blockId)
    {
        var clip = GameBlocks.GetBlockAudioClipFromItemID(blockId, BlockInteractionType.Destroy);
        if (clip == null)
        {
            return;
        }

        if (blockDestroy != null && blockDestroy.Clip == clip)
        {
            return;
        }

        if (blockDestroy != null)
        {
            blockDestroy.Stop();
        }

        blockDestroy = new AudioSource(clip);
    }


    private const float MinBlockDestroyTime = 0.175f;
    private float _time = MinBlockDestroyTime;
    public override void Update(Astronaut player)
    {
        if (Input.IsMouseButtonUp(MouseButton.Left))
        {
            _time = MinBlockDestroyTime;
            model?.SetAnimation(false);
        }

        if (!player.CanMove)
        {
            model?.SetAnimation(false);
            return;
        }

        Ray ray = new Ray(player.Position, player.Front, MaxDestroyDistance);
        VoxelPhysics.HitInfo hit;

        if (World.CurrentSector.Raycast(ray, out hit))
        {
            BlockSelector.IsVisible = true;
            AImedBlockElement.AimedBlock = hit.block;

            Vector3 selectorPos = hit.blockPositionIndex + hit.chunk.PositionWorld;
            BlockSelector.Instance.UpdatePosition(selectorPos,
                Block.GetDirectionFromNormal(hit.normal));

            if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                _time = MinBlockDestroyTime;
                model?.SetAnimation(true);
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


            lastInteractiveBlock = hit.block as InteractiveBlock;

            if (lastInteractiveBlock == null)
            {

                CenteredText.Hide();
                return;
            }

            UpdateInteractive(lastInteractiveBlock, player, hit.chunk, hit.position);
        }
        else
        {
            AImedBlockElement.AimedBlock = null;
            BlockSelector.IsVisible = false;
            CenteredText.Hide();
            if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                model?.SetAnimation(true);

            }

        }
    }

    private void DestroyBlock(VoxelPhysics.HitInfo hit)
    {

        hit.block.Durability = 0;
        hit.chunk.RemoveBlock(hit.blockPositionIndex, hit.normal);
        if (blockDestroy != null)
        {
            PickDestroySound(hit.block.BlockId);
            blockDestroy.Play();
        }

        else Debug.Error("blockDestroy was null!");
    }

    private void UpdateInteractive(InteractiveBlock block, Astronaut player, Chunk chunk, Vector3 hitPos)
    {
        var disSq = Vector3.DistanceSquared(player.Position, hitPos);

        if (disSq > InteractiveBlock.InteractionDistanceSquared)
        {
            CenteredText.Hide();
        }
        else
        {
            CenteredText.Show();
            if (ToggleManager.OpenedWindowsCount == 0)
            {
                if (Input.IsMouseButtonDown(MouseButton.Right))
                {
                    block.chunk = chunk;
                    block.Use(player);

                }
            }
        }
    }


}