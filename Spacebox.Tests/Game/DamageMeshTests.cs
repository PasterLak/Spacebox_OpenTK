using Engine;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Tests.Game;

[Collection(GameAssetsCollection.Name)]
public class DamageMeshTests
{
    private const short TestId = 4060;

    [Theory]
    [InlineData(10, 0)]
    [InlineData(8, 1)]
    [InlineData(1, 3)]
    public void Mesh_WritesDamageStageIntoEveryVertex(byte durability, int expectedStage)
    {
        var data = new BlockData("damage_mesh_test", "block") { Id = TestId, Id_string = "test:damage_mesh", Durability = 10, Mass = 1 };
        GameAssets.Blocks[TestId] = data;
        try
        {
            var block = new Block(data) { Durability = durability };
            var padded = new Block[Chunk.Size + 2, Chunk.Size + 2, Chunk.Size + 2];
            padded[5, 5, 5] = block;

            var mesh = new MeshGenerator(Vector3SByte.Zero, padded, false).GenerateMeshData();
            int vertices = mesh.VertexCount / BlockVertex.Words;

            Assert.True(vertices > 0);
            for (int i = 0; i < vertices; i++)
            {
                uint packed = (uint)BitConverter.SingleToInt32Bits(mesh.Vertices[i * BlockVertex.Words + 5]);
                Assert.Equal(expectedStage, (int)(packed >> 24) & BlockVertex.DamageMask);
            }
        }
        finally
        {
            GameAssets.Blocks.Remove(TestId);
        }
    }
}
