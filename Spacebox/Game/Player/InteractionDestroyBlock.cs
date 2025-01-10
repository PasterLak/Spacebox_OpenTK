using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpNBT;
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
    public override void OnEnable()
    {
        if (blockDestroy == null)
        {
            blockDestroy = new AudioSource(SoundManager.GetClip("blockDestroy"));
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
    }

    public override void Update(Astronaut player)
    {
        if (!player.CanMove)
        {
            if (Input.IsKeyDown(Keys.F))
            {
                if (lastInteractiveBlock != null)
                    lastInteractiveBlock.Use(player);
            }
            return;
        }

        Ray ray = new Ray(player.Position, player.Front, MaxDestroyDistance);
        VoxelPhysics.HitInfo hit;

        if (World.CurrentSector.Raycast(ray, out hit))
        {
            BlockSelector.IsVisible = true;
            AImedBlockElement.AimedBlock = hit.block;

            Vector3 selectorPos = hit.blockPosition + hit.chunk.PositionWorld;
            BlockSelector.Instance.UpdatePosition(selectorPos,
                Block.GetDirectionFromNormal(hit.normal));

            if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                hit.block.Durability = 0;
                hit.chunk.RemoveBlock(hit.blockPosition, hit.normal);

                if (blockDestroy != null)
                {
                  
                    blockDestroy.Play();
                }

                else Debug.Error("blockDestroy was null!");
            }

            lastInteractiveBlock = hit.block as InteractiveBlock;

            if (lastInteractiveBlock == null)
            {
                CenteredText.Hide();
                return;
            }

            UpdateInteractive(lastInteractiveBlock, player, hit.chunk);
        }
        else
        {
            AImedBlockElement.AimedBlock = null;
            BlockSelector.IsVisible = false;
        }
    }

    private void UpdateInteractive(InteractiveBlock block, Astronaut player, Chunk chunk)
    {

        CenteredText.Show();

        if (Input.IsKeyDown(Keys.F))
        {
            block.chunk = chunk;
            block.Use(player);
        }

    }
}