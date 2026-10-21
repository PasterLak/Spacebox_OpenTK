using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Rendering;
using Spacebox.Game.Resource;
using Spacebox.Game;

namespace Spacebox.Tests.Game;

public class InvisibleBlockTests
{
    private const short BarrierId = 4070;

    [Fact]
    public void Mesh_SingleCellHasSixFaces()
    {
        var vertices = new List<float>();
        var indices = new List<uint>();
        var occupied = new HashSet<Vector3i> { Vector3i.Zero };

        InvisibleBlockMesh.AddCell(Vector3i.Zero, null, occupied, vertices, indices);

        Assert.Equal(6 * 4 * InvisibleBlockMesh.FloatsPerVertex, vertices.Count);
        Assert.Equal(6 * 6, indices.Count);
    }

    [Fact]
    public void Mesh_SkipsFacesBetweenNeighbours()
    {
        var vertices = new List<float>();
        var indices = new List<uint>();
        var a = Vector3i.Zero;
        var b = new Vector3i(1, 0, 0);
        var occupied = new HashSet<Vector3i> { a, b };

        InvisibleBlockMesh.AddCell(a, null, occupied, vertices, indices);
        InvisibleBlockMesh.AddCell(b, null, occupied, vertices, indices);

        Assert.Equal(10 * 6, indices.Count);
        Assert.Equal((uint)(10 * 4 - 1), indices.Max());
    }

    [Fact]
    public void Unbreakable_OnlyCreativeCanDestroy()
    {
        var data = new BlockData("barrier_test", "invisible") { Id = BarrierId, Id_string = "test:barrier", Durability = 255, Mass = 1, Unbreakable = true };
        GameAssets.Blocks[BarrierId] = data;
        try
        {
            var block = new InvisibleBlock(data);

            Assert.True(UnbreakableBlocks.Is(block));
            Assert.True(UnbreakableBlocks.CanDestroy(block, GameMode.Creative));
            Assert.False(UnbreakableBlocks.CanDestroy(block, GameMode.Survival));
            Assert.False(UnbreakableBlocks.CanDestroy(block, GameMode.Spectator));
            Assert.True(UnbreakableBlocks.CanDestroy(new Block(), GameMode.Survival));
        }
        finally
        {
            GameAssets.Blocks.Remove(BarrierId);
        }
    }
}
