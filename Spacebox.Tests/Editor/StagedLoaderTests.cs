using Spacebox.Editor;

namespace Spacebox.Tests.Editor;

public class StagedLoaderTests
{
    [Fact]
    public void FirstTick_OnlyShowsTheFirstStep()
    {
        int runs = 0;
        var loader = new StagedLoader().Add("A", () => runs++).Add("B", () => runs++);

        loader.Tick();

        Assert.Equal(0, runs);
        Assert.Equal("A", loader.CurrentStep);
        Assert.Equal(0f, loader.Progress);
    }

    [Fact]
    public void Steps_RunOnePerTickInOrder()
    {
        var order = new List<string>();
        var loader = new StagedLoader().Add("A", () => order.Add("A")).Add("B", () => order.Add("B"));

        loader.Tick();
        loader.Tick();
        Assert.Equal("B", loader.CurrentStep);
        Assert.Equal(0.5f, loader.Progress);
        loader.Tick();

        Assert.True(loader.IsDone);
        Assert.Equal(new[] { "A", "B" }, order);
        Assert.Equal(1f, loader.Progress);
        loader.Tick();
        Assert.Equal(2, order.Count);
    }
}
