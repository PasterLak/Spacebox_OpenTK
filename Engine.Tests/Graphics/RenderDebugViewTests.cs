using Engine.Graphics.Debugging;

namespace Engine.Tests.Graphics;

public class RenderDebugViewTests
{
    [Fact]
    public void Step_CyclesThroughEveryModeAndWraps()
    {
        var mode = RenderDebugMode.Lit;
        var seen = new HashSet<RenderDebugMode>();
        foreach (var _ in Enum.GetValues<RenderDebugMode>())
        {
            seen.Add(mode);
            mode = RenderDebugView.Step(mode, 1);
        }

        Assert.Equal(RenderDebugMode.Lit, mode);
        Assert.Equal(Enum.GetValues<RenderDebugMode>().Length, seen.Count);
    }

    [Fact]
    public void Step_BackwardsFromLitGoesToLastMode()
    {
        var modes = Enum.GetValues<RenderDebugMode>();
        Assert.Equal(modes[^1], RenderDebugView.Step(RenderDebugMode.Lit, -1));
    }

    [Theory]
    [InlineData(RenderDebugMode.Normals, true)]
    [InlineData(RenderDebugMode.Depth, true)]
    [InlineData(RenderDebugMode.GBufferMask, true)]
    [InlineData(RenderDebugMode.Lit, false)]
    [InlineData(RenderDebugMode.Wireframe, false)]
    [InlineData(RenderDebugMode.Ssao, false)]
    public void BufferModes_AreDrawnFromTheSceneTargets(RenderDebugMode mode, bool expected)
    {
        Assert.Equal(expected, RenderDebugView.IsBufferMode(mode));
    }

    [Fact]
    public void EveryMode_HasADescription()
    {
        foreach (var mode in Enum.GetValues<RenderDebugMode>())
            Assert.False(string.IsNullOrWhiteSpace(RenderDebugView.Describe(mode)));
    }
}
