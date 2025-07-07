using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.Physics;

using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Spacebox.GUI;
using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Player.Interactions;

public class InteractionDefault : InteractionMode
{
    public override void OnEnable()
    {

    }

    public override void OnDisable()
    {
        //CenteredText.Hide();
    }

    public override void Update(Astronaut player)
    {
        Ray ray = new Ray(player.Position, player.Front, InteractiveBlock.InteractionDistance);
        HitInfo hit;

        if (World.CurrentSector.Raycast(ray, out hit))
        {
            if (hit.block == null) return;



            if (hit.block.Is<InteractiveBlock>(out var interactiveBlock))
            {
                InteractiveBlock.UpdateInteractive(interactiveBlock, player, hit.chunk, hit.position);

                if (hit.block.Is<StorageBlock>(out var storageBlock))
                {
                    //Debug.Log("Placed: " + ((Vector3i)(hit.blockPositionEntity )));
                    storageBlock.SetPositionInChunk(hit.blockPositionIndex);
                }
            }
            else
            {
               // CenteredText.Hide();
                return;
            }



        }
        else
        {
           // CenteredText.Hide();
        }
    }

}