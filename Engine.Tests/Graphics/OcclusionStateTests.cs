using Engine.Graphics.Occlusion;
using OpenTK.Mathematics;

namespace Engine.Tests.Graphics;

public class OcclusionStateTests
{
    [Fact]
    public void NewState_IsVisible()
    {
        var state = new OcclusionState();
        state.Touch(1, 0);
        Assert.True(state.Visible);
    }

    [Fact]
    public void HidesOnlyAfterTwoOccludedResults()
    {
        var state = new OcclusionState();
        state.Touch(1, 0);
        state.Apply(false);
        Assert.True(state.Visible);
        state.Apply(false);
        Assert.False(state.Visible);
    }

    [Fact]
    public void OneVisibleResult_ShowsAtOnce()
    {
        var state = new OcclusionState();
        state.Touch(1, 0);
        state.Apply(false);
        state.Apply(false);
        state.Apply(true);
        Assert.True(state.Visible);
        Assert.Equal(0, state.OccludedStreak);
    }

    [Fact]
    public void NotSeenLastFrame_BecomesVisibleAgain()
    {
        var state = new OcclusionState();
        state.Touch(1, 0);
        state.Apply(false);
        state.Apply(false);
        state.Touch(2, 0);
        Assert.False(state.Visible);

        state.Touch(5, 0);
        Assert.True(state.Visible);
    }

    [Fact]
    public void NewEpoch_ResetsToVisible()
    {
        var state = new OcclusionState();
        state.Touch(1, 0);
        state.Apply(false);
        state.Apply(false);
        state.Pending = true;

        state.Touch(2, 1);
        Assert.True(state.Visible);
        Assert.False(state.Pending);
    }

    [Fact]
    public void ForceVisible_ClearsTheOccludedStreak()
    {
        var state = new OcclusionState();
        state.Touch(1, 0);
        state.Apply(false);
        state.Apply(false);
        state.ForceVisible();
        Assert.True(state.Visible);
        Assert.Equal(0, state.OccludedStreak);
    }

    [Theory]
    [InlineData(false, 10f, false)]
    [InlineData(false, 100f, true)]
    [InlineData(true, 0f, true)]
    public void Culler_ResetsOnCameraChangeOrTeleport(bool cameraChanged, float moved, bool expected)
    {
        Assert.Equal(expected, OcclusionCuller.NeedsReset(cameraChanged, Vector3.Zero, new Vector3(moved, 0f, 0f)));
    }
}
