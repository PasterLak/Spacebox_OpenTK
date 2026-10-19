using Spacebox.Game;
using Spacebox.Game.Player;
using Xunit;

namespace Spacebox.Tests.Game;

public class HeldBlockMeshTests
{
    private static float[] Vertices(params (float x, float y, float z)[] points)
    {
        var vertices = new float[points.Length * HeldBlockMesh.FloatsPerVertex];
        for (int i = 0; i < points.Length; i++)
        {
            vertices[i * HeldBlockMesh.FloatsPerVertex] = points[i].x;
            vertices[i * HeldBlockMesh.FloatsPerVertex + 1] = points[i].y;
            vertices[i * HeldBlockMesh.FloatsPerVertex + 2] = points[i].z;
            vertices[i * HeldBlockMesh.FloatsPerVertex + 6] = 0.25f;
        }
        return vertices;
    }

    [Fact]
    public void Fit_KeepsModelsInsideOneBlock()
    {
        var vertices = Vertices((-0.2f, -0.5f, -0.2f), (0.2f, 0.3f, 0.2f));
        var copy = (float[])vertices.Clone();

        HeldBlockMesh.FitToUnitCube(vertices, HeldBlockMesh.FloatsPerVertex);

        Assert.Equal(copy, vertices);
    }

    [Fact]
    public void Fit_ScalesBigModelsDownToOneBlock()
    {
        var vertices = Vertices((-0.5f, -0.5f, -0.5f), (2.5f, 0.5f, 0.5f));

        HeldBlockMesh.FitToUnitCube(vertices, HeldBlockMesh.FloatsPerVertex);

        Assert.Equal(-0.5f, vertices[0], 4);
        Assert.Equal(0.5f, vertices[HeldBlockMesh.FloatsPerVertex], 4);
        Assert.Equal(-1f / 6f, vertices[1], 4);
        Assert.Equal(1f / 6f, vertices[HeldBlockMesh.FloatsPerVertex + 1], 4);
        Assert.Equal(0.25f, vertices[6], 4);
    }

    [Fact]
    public void Fit_CentersSmallModelsOutsideTheCell()
    {
        var vertices = Vertices((0.2f, 0f, 0f), (0.8f, 0.4f, 0.4f));

        HeldBlockMesh.FitToUnitCube(vertices, HeldBlockMesh.FloatsPerVertex);

        Assert.Equal(-0.3f, vertices[0], 4);
        Assert.Equal(0.3f, vertices[HeldBlockMesh.FloatsPerVertex], 4);
    }

    [Fact]
    public void ItemReceiver_AcceptsEveryRealItem()
    {
        Assert.True(ItemReceiver.CanReceive(new EquipmentItem("helmet", 1f) { Id = 5 }));
        Assert.True(ItemReceiver.CanReceive(new ConsumableItem(1, "nothing", 1f) { Id = 6 }));
        Assert.True(ItemReceiver.CanReceive(new Item(7, 1, "ore")));
        Assert.False(ItemReceiver.CanReceive(new Item(0, 1, "air")));
        Assert.False(ItemReceiver.CanReceive(null));
    }

    [Fact]
    public void ItemReceiver_OnlyEffectConsumablesAreApplied()
    {
        Assert.True(ItemReceiver.HasUseEffect(new ConsumableItem(1, "medkit", 1f) { OxygenAmount = 5 }));
        Assert.False(ItemReceiver.HasUseEffect(new ConsumableItem(1, "beer", 1f)));
    }
}
