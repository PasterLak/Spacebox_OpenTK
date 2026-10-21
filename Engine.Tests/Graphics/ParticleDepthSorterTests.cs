namespace Engine.Tests.Graphics;

public class ParticleDepthSorterTests
{
    [Fact]
    public void Sort_OrdersFarthestFirst()
    {
        var sorter = new ParticleDepthSorter();
        float[] distances = { 4f, 25f, 1f, 9f };
        sorter.Begin(distances.Length);
        for (int i = 0; i < distances.Length; i++) sorter.Set(i, distances[i]);
        sorter.Sort();

        Assert.Equal(new[] { 1, 3, 0, 2 }, Enumerable.Range(0, sorter.Count).Select(i => sorter[i]));
    }

    [Fact]
    public void Begin_ReusesAndGrowsBuffers()
    {
        var sorter = new ParticleDepthSorter();
        sorter.Begin(2);
        sorter.Set(0, 1f);
        sorter.Set(1, 2f);
        sorter.Sort();
        Assert.Equal(1, sorter[0]);

        sorter.Begin(100);
        for (int i = 0; i < 100; i++) sorter.Set(i, i);
        sorter.Sort();
        Assert.Equal(100, sorter.Count);
        Assert.Equal(99, sorter[0]);
        Assert.Equal(0, sorter[99]);
    }
}
