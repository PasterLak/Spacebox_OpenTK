using Engine.Graphics.Transparency;
using OpenTK.Mathematics;

namespace Engine.Tests.Graphics;

[Collection(GlobalStateCollection.Name)]
public class TransparentQueueTests
{
    private sealed class Recorder : ITransparentRenderable
    {
        private readonly List<string> _log;
        private readonly string _name;

        public Recorder(List<string> log, string name)
        {
            _log = log;
            _name = name;
        }

        public void RenderTransparent() => _log.Add(_name);
    }

    [Fact]
    public void TrySubmit_OutsideScope_ReturnsFalse()
    {
        TransparentQueue.Clear();
        var log = new List<string>();
        Assert.False(TransparentQueue.TrySubmit(new Recorder(log, "a"), Vector3.Zero));
        Assert.Equal(0, TransparentQueue.Count);
    }

    [Fact]
    public void EndScope_DrawsBackToFront()
    {
        TransparentQueue.Clear();
        var log = new List<string>();
        TransparentQueue.BeginScope(Vector3.Zero);
        TransparentQueue.TrySubmit(new Recorder(log, "near"), new Vector3(1, 0, 0));
        TransparentQueue.TrySubmit(new Recorder(log, "far"), new Vector3(10, 0, 0));
        TransparentQueue.TrySubmit(new Recorder(log, "mid"), new Vector3(0, 5, 0));
        TransparentQueue.EndScope();

        Assert.Equal(new[] { "far", "mid", "near" }, log);
        Assert.Equal(0, TransparentQueue.Count);
        Assert.False(TransparentQueue.IsCollecting);
    }

    [Fact]
    public void EndScope_OrdersByQueueFirst()
    {
        TransparentQueue.Clear();
        var log = new List<string>();
        TransparentQueue.BeginScope(Vector3.Zero);
        TransparentQueue.TrySubmit(new Recorder(log, "late-far"), new Vector3(100, 0, 0), 10);
        TransparentQueue.TrySubmit(new Recorder(log, "early-near"), new Vector3(1, 0, 0), 0);
        TransparentQueue.EndScope();

        Assert.Equal(new[] { "early-near", "late-far" }, log);
    }

    [Fact]
    public void EqualDistances_KeepSubmitOrder()
    {
        TransparentQueue.Clear();
        var log = new List<string>();
        TransparentQueue.BeginScope(Vector3.Zero);
        for (int i = 0; i < 20; i++)
            TransparentQueue.TrySubmit(new Recorder(log, i.ToString()), Vector3.UnitX);
        TransparentQueue.EndScope();

        Assert.Equal(Enumerable.Range(0, 20).Select(i => i.ToString()), log);
    }

    [Fact]
    public void NestedScope_FlushesOnlyItsOwnEntries()
    {
        TransparentQueue.Clear();
        var log = new List<string>();
        TransparentQueue.BeginScope(Vector3.Zero);
        TransparentQueue.TrySubmit(new Recorder(log, "outer"), Vector3.UnitX);

        TransparentQueue.BeginScope(new Vector3(100, 0, 0));
        TransparentQueue.TrySubmit(new Recorder(log, "inner"), Vector3.UnitX);
        TransparentQueue.EndScope();
        Assert.Equal(new[] { "inner" }, log);

        TransparentQueue.TrySubmit(new Recorder(log, "outer-far"), new Vector3(50, 0, 0));
        TransparentQueue.EndScope();
        Assert.Equal(new[] { "inner", "outer-far", "outer" }, log);
    }

    [Fact]
    public void Queue_GrowsBeyondInitialCapacity()
    {
        TransparentQueue.Clear();
        int drawn = 0;
        var counter = new Counter(() => drawn++);
        TransparentQueue.BeginScope(Vector3.Zero);
        for (int i = 0; i < 1000; i++)
            TransparentQueue.TrySubmit(counter, new Vector3(i, 0, 0));
        TransparentQueue.EndScope();
        Assert.Equal(1000, drawn);
    }

    private sealed class Counter : ITransparentRenderable
    {
        private readonly Action _action;
        public Counter(Action action) => _action = action;
        public void RenderTransparent() => _action();
    }
}
