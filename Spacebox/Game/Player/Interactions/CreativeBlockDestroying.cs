using Engine.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player.Interactions;

public sealed class CreativeBlockDestroying : IPassiveInteraction
{
    private const float MaxDestroyDistance = 6f;
    private const float RepeatInterval = 0.175f;

    private readonly RepeatingAction _destroyAction = new RepeatingAction("block_destroy", RepeatInterval);

    public void Update(LocalAstronaut player, InteractionMode activeInteraction)
    {
        if (activeInteraction != null && activeInteraction.UsesPrimaryAction) return;
        if (!player.CanMove) return;
        if (!_destroyAction.Poll()) return;
        if (World.CurrentSector == null) return;

        var ray = new Ray(player.Position, player.Front, MaxDestroyDistance);
        if (!World.CurrentSector.Raycast(ray, out HitInfo hit) || hit.block == null) return;

        BlockDestroyer.DestroyInstantly(hit, player);
    }
}
