using Engine.Networking;
using OpenTK.Mathematics;

namespace Engine.Tests.Networking;

public class TransformInterpolatorTests
{
    private static TransformState State(float time, float x) => new TransformState(time, new Vector3(x, 0f, 0f), Quaternion.Identity);

    private static float SampleX(TransformInterpolator interpolator, float localTime, float delay)
    {
        Assert.True(interpolator.TrySample(localTime, delay, out var position, out _));
        return position.X;
    }

    [Fact]
    public void Sample_UsesSenderTimeSoArrivalJitterDoesNotMatter()
    {
        var interpolator = new TransformInterpolator(32) { ExpectedInterval = 0.05f };
        interpolator.Add(10.00f, State(0.00f, 0f));
        interpolator.Add(10.09f, State(0.05f, 1f));
        interpolator.Add(10.10f, State(0.10f, 2f));
        interpolator.Add(10.15f, State(0.15f, 3f));

        Assert.Equal(10f, interpolator.ClockOffset, 3);
        Assert.Equal(2.5f, SampleX(interpolator, 10.225f, 0.1f), 3);
    }

    [Fact]
    public void Add_InsertsHoldAfterIdleGap()
    {
        var interpolator = new TransformInterpolator(32) { ExpectedInterval = 0.05f };
        interpolator.Add(0f, State(0f, 0f));
        interpolator.Add(2f, State(2f, 1f));

        Assert.Equal(0f, SampleX(interpolator, 1.9f, 0f), 3);
        Assert.Equal(0.5f, SampleX(interpolator, 1.975f, 0f), 3);
    }

    [Fact]
    public void Add_SnapsOnTeleport()
    {
        var interpolator = new TransformInterpolator(32) { ExpectedInterval = 0.05f, SnapDistance = 20f };
        interpolator.Add(0f, State(0f, 0f));
        interpolator.Add(0.05f, State(0.05f, 100f));

        Assert.Equal(1, interpolator.Count);
        Assert.Equal(100f, SampleX(interpolator, 0.06f, 0.1f), 3);
    }

    [Fact]
    public void Add_ResetsWhenSenderClockRestarts()
    {
        var interpolator = new TransformInterpolator(32) { ExpectedInterval = 0.05f };
        interpolator.Add(100f, State(90f, 0f));
        interpolator.Add(100.05f, State(90.05f, 1f));
        interpolator.Add(101f, State(0.1f, 2f));

        Assert.Equal(1, interpolator.Count);
        Assert.Equal(2f, SampleX(interpolator, 101.2f, 0.1f), 3);
    }

    [Fact]
    public void Add_IgnoresDuplicatesAndKeepsCapacity()
    {
        var interpolator = new TransformInterpolator(4) { ExpectedInterval = 0.05f };
        for (int i = 0; i < 10; i++) interpolator.Add(i * 0.05f, State(i * 0.05f, i * 0.1f));
        interpolator.Add(1f, State(0.45f, 5f));

        Assert.Equal(4, interpolator.Count);
    }
}
