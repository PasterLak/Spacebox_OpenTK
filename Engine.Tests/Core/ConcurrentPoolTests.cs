namespace Engine.Tests.Core;

public class ConcurrentPoolTests
{
    private sealed class Item
    {
        public int Resets;
    }

    [Fact]
    public void Rent_ReusesReturnedObject()
    {
        var pool = new ConcurrentPool<Item>(() => new Item());

        var first = pool.Rent();
        pool.Return(first);
        var second = pool.Rent();

        Assert.Same(first, second);
    }

    [Fact]
    public void Return_InvokesReset()
    {
        var pool = new ConcurrentPool<Item>(() => new Item(), item => item.Resets++);

        var item = pool.Rent();
        pool.Return(item);

        Assert.Equal(1, item.Resets);
    }

    [Fact]
    public void Return_DropsObjectsAboveMaxRetained()
    {
        var pool = new ConcurrentPool<Item>(() => new Item(), null, 2);

        pool.Return(new Item());
        pool.Return(new Item());
        pool.Return(new Item());

        Assert.Equal(2, pool.RetainedCount);
    }

    [Fact]
    public void Return_IgnoresNull()
    {
        var pool = new ConcurrentPool<Item>(() => new Item());

        pool.Return(null!);

        Assert.Equal(0, pool.RetainedCount);
    }

    [Fact]
    public void Rent_CreatesNewObjectWhenEmpty()
    {
        int created = 0;
        var pool = new ConcurrentPool<Item>(() => { created++; return new Item(); });

        var a = pool.Rent();
        var b = pool.Rent();

        Assert.NotSame(a, b);
        Assert.Equal(2, created);
    }

    [Fact]
    public void ParallelRentReturn_KeepsRetainedCountWithinLimit()
    {
        var pool = new ConcurrentPool<Item>(() => new Item(), null, 4);

        Parallel.For(0, 1000, _ =>
        {
            var item = pool.Rent();
            pool.Return(item);
        });

        Assert.InRange(pool.RetainedCount, 1, 4);
    }

    [Fact]
    public void Constructor_RejectsNullFactory()
    {
        Assert.Throws<ArgumentNullException>(() => new ConcurrentPool<Item>(null!));
    }
}
