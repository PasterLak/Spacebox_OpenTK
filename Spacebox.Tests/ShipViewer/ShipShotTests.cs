using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Game.ShipViewer;

namespace Spacebox.Tests.ShipViewer;

public class ShipShotTests
{
    private const int Size = Chunk.Size;
    private const short WallId = 4001;

    [Fact]
    public void Shot_DamagesFirstBlockWithoutDestroyingIt()
    {
        var (grid, blocks) = Grid();
        blocks[10, 5, 5] = Wall(100);

        var shot = ShipShot.Fire(grid, new Vector3(0.5f, 5.5f, 5.5f), Vector3.UnitX, 50f, Projectile(damage: 30, penetration: 255, maxBlocks: 3));

        Assert.True(shot.Hit);
        Assert.Equal(70, blocks[10, 5, 5].Durability);
        Assert.Equal(new Vector3(10f, 5.5f, 5.5f), shot.End);
        Assert.Single(shot.ChangedCells);
    }

    [Fact]
    public void Shot_DestroysBlocksUntilDamageIsSpent()
    {
        var (grid, blocks) = Grid();
        blocks[10, 5, 5] = Wall(20);
        blocks[11, 5, 5] = Wall(20);
        blocks[12, 5, 5] = Wall(20);

        var shot = ShipShot.Fire(grid, new Vector3(0.5f, 5.5f, 5.5f), Vector3.UnitX, 50f, Projectile(damage: 50, penetration: 255, maxBlocks: 5));

        Assert.True(blocks[10, 5, 5].IsAir);
        Assert.True(blocks[11, 5, 5].IsAir);
        Assert.Equal(10, blocks[12, 5, 5].Durability);
        Assert.Equal(12f, shot.End.X, 3);
    }

    [Fact]
    public void Shot_StopsAtMaximumDestroyedBlocks()
    {
        var (grid, blocks) = Grid();
        blocks[10, 5, 5] = Wall(5);
        blocks[11, 5, 5] = Wall(5);

        ShipShot.Fire(grid, new Vector3(0.5f, 5.5f, 5.5f), Vector3.UnitX, 50f, Projectile(damage: 100, penetration: 255, maxBlocks: 1));

        Assert.True(blocks[10, 5, 5].IsAir);
        Assert.Equal(5, blocks[11, 5, 5].Durability);
    }

    [Fact]
    public void Shot_WithoutEnoughPenetration_LeavesBlockIntact()
    {
        var (grid, blocks) = Grid();
        blocks[10, 5, 5] = Wall(100);

        var shot = ShipShot.Fire(grid, new Vector3(0.5f, 5.5f, 5.5f), Vector3.UnitX, 50f, Projectile(damage: 100, penetration: 50, maxBlocks: 1));

        Assert.True(shot.Hit);
        Assert.Equal(100, blocks[10, 5, 5].Durability);
        Assert.Empty(shot.ChangedCells);
    }

    [Fact]
    public void Shot_RicochetsAtFlatAngles()
    {
        var (grid, blocks) = Grid();
        for (int x = 0; x < Size; x++) blocks[x, 4, 5] = Wall(100);

        var direction = Vector3.Normalize(new Vector3(1f, -0.1f, 0f));
        var shot = ShipShot.Fire(grid, new Vector3(0.5f, 5.9f, 5.5f), direction, 20f, Projectile(damage: 30, penetration: 255, maxBlocks: 1, ricochetAngle: 20, ricochets: 1));

        Assert.True(shot.Hit);
        Assert.Empty(shot.ChangedCells);
        Assert.Equal(3, shot.Path.Count);
        Assert.True(shot.End.Y > 5f);
    }

    [Fact]
    public void Shot_CrossesChunkBorders()
    {
        var (grid, _, far) = TwoChunkGrid();
        far[2, 5, 5] = Wall(10);

        var shot = ShipShot.Fire(grid, new Vector3(0.5f, 5.5f, 5.5f), Vector3.UnitX, 80f, Projectile(damage: 100, penetration: 255, maxBlocks: 1));

        Assert.True(far[2, 5, 5].IsAir);
        Assert.Equal(Size + 2f, shot.End.X, 3);
        var chunks = new HashSet<Vector3SByte>();
        grid.CollectAffectedChunks(shot.ChangedCells, chunks);
        Assert.Contains(new Vector3SByte(1, 0, 0), chunks);
    }

    [Fact]
    public void Miss_EndsAtRange()
    {
        var (grid, _) = Grid();

        var shot = ShipShot.Fire(grid, new Vector3(0.5f, 5.5f, 5.5f), Vector3.UnitY, 10f, Projectile(damage: 10, penetration: 255, maxBlocks: 1));

        Assert.False(shot.Hit);
        Assert.Equal(15.5f, shot.End.Y, 3);
    }

    private static (ShipBlockGrid Grid, Block[,,] Blocks) Grid()
    {
        var blocks = AirChunk();
        var chunks = new Dictionary<Vector3SByte, Block[,,]> { [new Vector3SByte(0, 0, 0)] = blocks };
        return (new ShipBlockGrid(chunks, Air), blocks);
    }

    private static (ShipBlockGrid Grid, Block[,,] Near, Block[,,] Far) TwoChunkGrid()
    {
        var near = AirChunk();
        var far = AirChunk();
        var chunks = new Dictionary<Vector3SByte, Block[,,]>
        {
            [new Vector3SByte(0, 0, 0)] = near,
            [new Vector3SByte(1, 0, 0)] = far
        };
        return (new ShipBlockGrid(chunks, Air), near, far);
    }

    private static Block[,,] AirChunk()
    {
        var blocks = new Block[Size, Size, Size];
        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
        for (int z = 0; z < Size; z++)
            blocks[x, y, z] = Air();
        return blocks;
    }

    private static Block Air() => new Block(0, Block.DefaultDirection, 0, 0, true, true, false, false);

    private static Block Wall(byte durability) => new Block(WallId, Block.DefaultDirection, 1, durability, false, false, false, false);

    private static ProjectileParameters Projectile(int damage, int penetration, int maxBlocks, int ricochetAngle = 0, int ricochets = 0)
    {
        return new ProjectileParameters(1, new ProjectileJSON
        {
            ID = "test",
            Speed = 50,
            DamageBlocks = damage,
            Penetration = penetration,
            MaximumBlocksCanDestroy = maxBlocks,
            RicochetAngle = ricochetAngle,
            PossibleRicochets = ricochets
        });
    }
}
