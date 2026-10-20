using Spacebox.Game.Physics;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public interface IExplosiveBlock
{
    ExplosiveBlockData Explosion { get; }
}

public static class ExplosiveTrigger
{
    public static bool Detonate(HitInfo hit, ExplosiveBlockData explosion)
    {
        var explosions = World.Instance?.Explosions;
        if (explosion == null || explosions == null || hit.chunk == null) return false;

        explosions.Detonate(hit.chunk, hit.blockPositionIndex, explosion, 0f);
        return true;
    }
}
