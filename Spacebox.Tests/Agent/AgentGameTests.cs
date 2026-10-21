using OpenTK.Mathematics;
using Spacebox.Agent;

namespace Spacebox.Tests.Agent;

public class AgentGameTests
{
    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(90f, 30f)]
    [InlineData(-135f, -45f)]
    [InlineData(179f, 80f)]
    public void Angles_RoundTripThroughFrontVector(float yaw, float pitch)
    {
        var front = AgentGame.FrontFromAngles(yaw, pitch);

        Assert.Equal(1f, front.Length, 4);
        Assert.Equal(yaw, AgentGame.Yaw(front), 2);
        Assert.Equal(pitch, AgentGame.Pitch(front), 2);
    }

    [Fact]
    public void ZeroYaw_LooksAlongNegativeZ_LikeTheCamera()
    {
        var front = AgentGame.FrontFromAngles(0f, 0f);

        Assert.Equal(-1f, front.Z, 4);
        Assert.Equal(90f, AgentGame.Yaw(Vector3.UnitX), 3);
    }

    [Fact]
    public void Vec_RoundsToMillimeters()
    {
        Assert.Equal(new[] { 1.235f, -2f, 0.001f }, AgentGame.Vec(new Vector3(1.23456f, -2f, 0.0009f)));
    }
}
