using Engine;
using OpenTK.Mathematics;

namespace Engine.Tests;

public class OrbitViewCameraTests
{
    [Fact]
    public void ComputeOffset_ZeroAngles_LooksFromPositiveZ()
    {
        var offset = OrbitViewCamera.ComputeOffset(0f, 0f, 5f);

        Assert.True((offset - new Vector3(0, 0, 5)).Length < 1e-4f);
    }

    [Fact]
    public void ComputeOffset_KeepsDistance()
    {
        var offset = OrbitViewCamera.ComputeOffset(37f, 52f, 3f);

        Assert.Equal(3f, offset.Length, 4);
        Assert.True(offset.Y > 0f);
    }

    [Fact]
    public void FitHalfAngle_NarrowViewUsesHorizontalFov()
    {
        float halfY = MathHelper.DegreesToRadians(22.5f);

        Assert.Equal(halfY, OrbitViewCamera.FitHalfAngle(halfY, 2f), 5);
        Assert.True(OrbitViewCamera.FitHalfAngle(halfY, 0.5f) < halfY);
    }
}
