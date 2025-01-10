using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Physics;
using Spacebox.FPS;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Spacebox.GUI;

namespace Spacebox.Game.Player;

public class InteractionDefault : InteractionMode
{
    public override void OnEnable()
    {
        
    }

    public override void OnDisable()
    {
        CenteredText.Hide();
    }

    public override void Update(Astronaut player)
    {
        Ray ray = new Ray(player.Position, player.Front, 3);
        VoxelPhysics.HitInfo hit;

        if (World.CurrentSector.Raycast(ray, out hit))
        {
            if(hit.block == null)  return;

            var interactiveBlock = hit.block as InteractiveBlock;

            if (interactiveBlock == null)
            {
                CenteredText.Hide(); 
                return;
            }

            UpdateInteractive(interactiveBlock, player, hit.chunk);
        }
        else
        {
            CenteredText.Hide();
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