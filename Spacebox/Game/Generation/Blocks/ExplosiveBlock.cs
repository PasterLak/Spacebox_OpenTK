using Spacebox.Game.Physics;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public class ExplosiveBlock : Block, IProjectileReactive
{
    private readonly ExplosiveBlockData _data;

    public ExplosiveBlockData Data => _data;

    public ExplosiveBlock(BlockData blockData) : base(blockData)
    {
        _data = blockData as ExplosiveBlockData ?? new ExplosiveBlockData(blockData, null);
    }

    public bool OnProjectileHit(HitInfo hit)
    {
        var explosions = World.Instance?.Explosions;
        if (explosions == null || hit.chunk == null) return false;

        explosions.Detonate(hit.chunk, hit.blockPositionIndex, _data, 0f);
        return true;
    }
}
