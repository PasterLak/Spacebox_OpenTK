namespace Engine.Tests.Core;

public class PoolTests
{
    private sealed class Item
    {
        public bool Active;
        public int Resets;
    }

    [Fact]
    public void Take_ReusesReleasedObjects()
    {
        var pool = new Pool<Item>(1);

        var first = pool.Take();
        pool.Release(first);
        var second = pool.Take();

        Assert.Same(first, second);
        Assert.Equal(1, pool.TotalObjects);
    }

    [Fact]
    public void Take_ExpandsWhenEmpty()
    {
        var pool = new Pool<Item>(1);

        pool.Take();
        pool.Take();

        Assert.Equal(2, pool.TotalObjects);
        Assert.Equal(0, pool.AvailableObjects);
    }

    [Fact]
    public void Take_WithoutAutoExpand_ThrowsWhenEmpty()
    {
        var pool = new Pool<Item>(1, autoExpand: false);
        pool.Take();

        Assert.Throws<InvalidOperationException>(() => pool.Take());
    }

    [Fact]
    public void Release_TwiceIsIgnored()
    {
        var pool = new Pool<Item>(0, resetFunc: i => i.Resets++);
        var item = pool.Take();

        pool.Release(item);
        pool.Release(item);

        Assert.Equal(1, item.Resets);
        Assert.Equal(1, pool.AvailableObjects);
    }

    [Fact]
    public void SetActive_FollowsTakeAndRelease()
    {
        var pool = new Pool<Item>(1, setActiveFunc: (i, active) => i.Active = active);
        var item = pool.Take();
        Assert.True(item.Active);

        pool.Release(item);
        Assert.False(item.Active);
    }

    [Fact]
    public void Clear_CallsCleanupForAll()
    {
        int cleaned = 0;
        var pool = new Pool<Item>(3);
        pool.Take();

        pool.Clear(_ => cleaned++);

        Assert.Equal(3, cleaned);
        Assert.Equal(0, pool.TotalObjects);
    }

    [Fact]
    public void Release_Null_Throws()
    {
        var pool = new Pool<Item>(0);

        Assert.Throws<ArgumentNullException>(() => pool.Release(null!));
    }
}
