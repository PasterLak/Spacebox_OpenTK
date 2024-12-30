using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player;

public class InteractionDestroyBlock : InteractionMode
{
    private const byte MaxDestroyDistance = 6;

    public override void Update(Astronaut player)
    {
        if (!Input.IsMouseButtonDown(MouseButton.Left)) return;

        Ray ray = new Ray(player.Position, player.Front, MaxDestroyDistance);
        VoxelPhysics.HitInfo hit;
        
        if (World.CurrentSector.Raycast(ray, out hit))
        {
            hit.chunk.RemoveBlock(hit.blockPosition, Vector3SByte.CreateFrom(hit.normal));
        }
        else
        {
        }
    }
}