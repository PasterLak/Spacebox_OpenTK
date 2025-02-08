﻿using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.Animation;
using Engine.Audio;
using Engine.Physics;
using Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.GUI;
using Client;


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

        model.Animator.AddAnimation(new MoveAnimation(model.Position, model.Position + new Vector3(0.005f, 0, 0), 0.05f, true));
    }

    public override void OnDisable()
    {
        BlockSelector.IsVisible = false;
        CenteredText.Hide();

        selectedItemSlot = null;
        model.Animator.Clear();
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
        HitInfo hit;

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


            if (hit.block.Is<InteractiveBlock>(out var b))
            {
                lastInteractiveBlock = b;
                InteractiveBlock.UpdateInteractive(lastInteractiveBlock, player, hit.chunk, hit.position);
            }
            else
            {
                CenteredText.Hide();

            }


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

    private void DestroyBlock(HitInfo hit)
    {

        hit.block.Durability = 0;
      //  hit.chunk.RemoveBlock(hit.blockPositionIndex, hit.normal);

        var x = (hit.chunk.PositionIndex * Chunk.Size);
        var localPos = new Vector3(x.X + hit.blockPositionIndex.X, x.Y + hit.blockPositionIndex.Y, x.Z + hit.blockPositionIndex.Z);
        hit.chunk.SpaceEntity.RemoveBlockAtLocal(hit.blockPositionEntity, hit.normal);
       
        if (ClientNetwork.Instance != null)
        {
            ClientNetwork.Instance.SendBlockDestroyed((short)localPos.X, (short)localPos.Y, (short)localPos.Z);
        }
        if (blockDestroy != null)
        {
            PickDestroySound(hit.block.BlockId);
            blockDestroy.Play();
        }

        else Debug.Error("blockDestroy was null!");
    }


    public override void Render(Astronaut player)
    {
       
    }
}