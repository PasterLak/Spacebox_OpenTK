using Spacebox.Game.Physics;

namespace Spacebox.Game.Generation.Blocks;

public interface IProjectileReactive
{
    bool OnProjectileHit(HitInfo hit);
}
