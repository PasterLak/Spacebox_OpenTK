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

public abstract class InteractionDestroyBlock : InteractionMode
{
    protected const byte MaxDestroyDistance = 6;
    protected static AudioSource blockDestroy;
    protected ItemSlot selectedItemSlot;
    protected AnimatedItemModel model;
    Random r = new Random();
    protected InteractionDestroyBlock(ItemSlot itemSlot)
    {
        AllowReload = true;
        selectedItemSlot = itemSlot;
        model = GameAssets.ItemModels[itemSlot.Item.Id] as AnimatedItemModel;
    }

    protected void PickDestroySound(short blockId)
    {
        var clip = GameAssets.GetBlockAudioClipFromItemID(blockId, BlockInteractionType.Destroy);
        if (clip == null) return;
        if (blockDestroy != null && blockDestroy.Clip == clip) return;
        blockDestroy?.Stop();
        blockDestroy = new AudioSource(clip);
    }

    protected void DestroyBlock(HitInfo hit, Astronaut player)
    {
        hit.block.Durability = 0;
        hit.chunk.SpaceEntity.RemoveBlockAtLocal(hit.blockPositionEntity, hit.normal);
        var x = hit.chunk.PositionIndex * Chunk.Size;
        var localPos = new Vector3(x.X + hit.blockPositionIndex.X, x.Y + hit.blockPositionIndex.Y, x.Z + hit.blockPositionIndex.Z);
        ClientNetwork.Instance?.SendBlockDestroyed((short)localPos.X, (short)localPos.Y, (short)localPos.Z);
        player.PlayerStatistics.BlocksDestroyed++;
        
        if (blockDestroy != null)
        {
            PickDestroySound(hit.block.Id);
           
            blockDestroy.Pitch = r.Next(9, 12)  * 0.1f; 
            blockDestroy.Play();
        }
        else Debug.Error("blockDestroy was null!");
    }

    protected void UpdateBlockSelector(HitInfo hit)
    {
        BlockSelector.IsVisible = true;
        AImedBlockElement.AimedBlock = hit.block;
        Vector3 selectorPos = hit.blockPositionIndex + hit.chunk.PositionWorld;
        BlockSelector.Instance.UpdatePosition(selectorPos, Block.GetDirectionFromNormal(hit.normal));
    }

    public override void OnDisable()
    {
        BlockSelector.IsVisible = false;
       // CenteredText.Hide();
        selectedItemSlot = null;
        model?.Animator.Clear();
    }
}
