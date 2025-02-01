using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.Physics;

using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Spacebox.GUI;
using Engine;


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
        Ray ray = new Ray(player.Position, player.Front, InteractiveBlock.InteractionDistance);
        HitInfo hit;

        if (World.CurrentSector.Raycast(ray, out hit))
        {
            if (hit.block == null) return;

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