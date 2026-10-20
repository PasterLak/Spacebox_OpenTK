using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Tests.Generation;

public class AsteroidApproachTests
{
    [Fact]
    public void ApproachPoint_IsOutsideTheSurfaceOnThePlayersSide()
    {
        var target = new AsteroidTarget(new Vector3(100, 0, 0), 30f);

        var point = AsteroidApproach.ApproachPoint(target, Vector3.Zero);

        Assert.Equal(100f - 30f - AsteroidApproach.SurfaceMargin, point.X, 3);
        Assert.Equal(0f, point.Y, 3);
        Assert.Equal(0f, point.Z, 3);
    }

    [Fact]
    public void ApproachPoint_FromTheCenter_StillLeavesTheAsteroid()
    {
        var target = new AsteroidTarget(new Vector3(5, 5, 5), 20f);

        var point = AsteroidApproach.ApproachPoint(target, target.Center);

        Assert.True((point - target.Center).Length > target.Radius);
    }

    [Fact]
    public void SurfaceDistance_IsZeroInside()
    {
        var target = new AsteroidTarget(Vector3.Zero, 10f);

        Assert.Equal(0f, AsteroidApproach.SurfaceDistance(target, new Vector3(3, 0, 0)));
        Assert.Equal(15f, AsteroidApproach.SurfaceDistance(target, new Vector3(25, 0, 0)), 3);
    }

    [Fact]
    public void AsteroidYouAreAt_IsSkipped_NearestOtherIsChosen()
    {
        var current = new AsteroidTarget(Vector3.Zero, 20f);
        var near = new AsteroidTarget(new Vector3(200, 0, 0), 20f);
        var far = new AsteroidTarget(new Vector3(900, 0, 0), 50f);
        var player = new Vector3(25, 0, 0);

        Assert.False(AsteroidApproach.IsBetter(current, player, float.MaxValue, out _));
        Assert.True(AsteroidApproach.IsBetter(near, player, float.MaxValue, out float nearDistance));
        Assert.False(AsteroidApproach.IsBetter(far, player, nearDistance, out _));
    }
}
