using Spacebox.Game.Physics;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public class ExplosiveBlock : Block, IProjectileReactive, IExplosiveBlock
{
    private readonly ExplosiveBlockData _data;

    public ExplosiveBlockData Data => _data;
    public ExplosiveBlockData Explosion => _data;

    public ExplosiveBlock(BlockData blockData) : base(blockData)
    {
        _data = blockData as ExplosiveBlockData ?? new ExplosiveBlockData(blockData, null);
    }

    public bool OnProjectileHit(HitInfo hit)
    {
        return ExplosiveTrigger.Detonate(hit, _data);
    }
}
