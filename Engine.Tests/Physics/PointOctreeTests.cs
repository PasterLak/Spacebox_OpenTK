using OpenTK.Mathematics;

namespace Engine.Tests.Physics;

public class PointOctreeTests
{
    [Fact]
    public void Add_ThenGetNearby_FindsOnlyCloseObjects()
    {
        var tree = new PointOctree<string>(64f, Vector3.Zero, 1f);
        tree.Add("near", new Vector3(1, 1, 1));
        tree.Add("far", new Vector3(20, 20, 20));

        var found = tree.GetNearby(Vector3.Zero, 5f);

        Assert.Equal(new[] { "near" }, found);
        Assert.Equal(2, tree.Count);
    }

    [Fact]
    public void Add_OutsideInitialSize_GrowsTree()
    {
        var tree = new PointOctree<string>(8f, Vector3.Zero, 1f);
        tree.Add("outside", new Vector3(500, -300, 100));

        Assert.Contains("outside", tree.GetNearby(new Vector3(500, -300, 100), 1f));
    }

    [Fact]
    public void Remove_DeletesObject()
    {
        var tree = new PointOctree<string>(64f, Vector3.Zero, 1f);
        tree.Add("a", Vector3.One);
        tree.Add("b", new Vector3(2));

        Assert.True(tree.Remove("a"));
        Assert.False(tree.Remove("a"));

        Assert.Equal(1, tree.Count);
        Assert.Equal(new[] { "b" }, tree.GetAll());
    }

    [Fact]
    public void ManyObjects_AllRetrievable()
    {
        var tree = new PointOctree<int>(32f, Vector3.Zero, 1f);
        for (int i = 0; i < 200; i++)
            tree.Add(i, new Vector3(i % 10, i / 10 % 10, i / 100));

        Assert.Equal(200, tree.Count);
        Assert.Equal(200, tree.GetAll().Count);

        var list = new List<int>();
        Assert.True(tree.GetNearbyNonAlloc(Vector3.Zero, 0.5f, list));
        Assert.Equal(new[] { 0 }, list);
    }
}
