using Engine.Physics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Player;

public static class PlayerTargeting
{
    public static bool TryFindLookedAt(LocalAstronaut player, float range, out RemoteAstronaut target)
    {
        target = null;
        var physics = World.Instance?.Physics;
        if (player == null || physics == null) return false;

        var ray = new Ray(player.Position, player.Front, range);
        if (!physics.Raycast(ray, player.Body, out var hit) || hit.Body?.Owner is not RemoteAstronaut remote) return false;

        var sector = World.CurrentSector;
        if (sector != null && sector.Raycast(new Ray(player.Position, player.Front, hit.Distance), out var block) && block.block != null) return false;

        target = remote;
        return true;
    }
}
