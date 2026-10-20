using Engine.Audio;
using OpenTK.Mathematics;

namespace Engine.Tests.Audio;

public class ListenerVelocityTests
{
    private const float SpeedOfSound = 343.3f;

    [Fact]
    public void NormalMovement_GivesVelocity()
    {
        var velocity = ListenerVelocity.Compute(new Vector3(1f, 0f, 0f), 0.1f, SpeedOfSound);

        Assert.Equal(10f, velocity.X, 3);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-0.1f)]
    public void ZeroOrNegativeFrameTime_GivesZero(float deltaTime)
    {
        Assert.Equal(Vector3.Zero, ListenerVelocity.Compute(new Vector3(1f, 0f, 0f), deltaTime, SpeedOfSound));
    }

    [Fact]
    public void NonFiniteMovement_GivesZero()
    {
        Assert.Equal(Vector3.Zero, ListenerVelocity.Compute(new Vector3(float.NaN, 0f, 0f), 0.016f, SpeedOfSound));
    }

    [Fact]
    public void Teleport_GivesZero()
    {
        Assert.Equal(Vector3.Zero, ListenerVelocity.Compute(new Vector3(500f, 0f, 0f), 0.016f, SpeedOfSound));
    }

    [Fact]
    public void FastMovement_IsCappedBelowSpeedOfSound()
    {
        var velocity = ListenerVelocity.Compute(new Vector3(10f, 0f, 0f), 0.016f, SpeedOfSound);

        Assert.Equal(SpeedOfSound * ListenerVelocity.MaxSpeedFraction, velocity.Length, 2);
    }
}
