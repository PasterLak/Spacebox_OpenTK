using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Common.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player;

public class InteractionDestroyBlock : InteractionMode
{
    private const byte MaxDestroyDistance = 6;

    private static AudioSource blockDestroy;


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
    }

    public override void Update(Astronaut player)
    {
        Ray ray = new Ray(player.Position, player.Front, MaxDestroyDistance);
        VoxelPhysics.HitInfo hit;

        if (World.CurrentSector.Raycast(ray, out hit))
        {
            BlockSelector.IsVisible = true;

            Vector3 selectorPos = hit.blockPosition + hit.chunk.PositionWorld;
            BlockSelector.Instance.UpdatePosition(selectorPos,
                Block.GetDirectionFromNormal(hit.normal));

            if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                hit.chunk.RemoveBlock(hit.blockPosition, hit.normal);

                if (blockDestroy != null)
                {
                  
                    blockDestroy.Play();
                }

                else Debug.Error("blockDestroy was null!");
            }
        }
        else
        {
            BlockSelector.IsVisible = false;
        }
    }
}