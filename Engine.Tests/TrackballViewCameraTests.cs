using Engine;
using OpenTK.Mathematics;

namespace Engine.Tests;

public class TrackballViewCameraTests
{
    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(40f, 25f)]
    [InlineData(-120f, -60f)]
    public void SetAngles_MatchesOrbitCamera(float yaw, float pitch)
    {
        var trackball = new TrackballViewCamera();
        trackball.SetAngles(yaw, pitch);
        trackball.Frame(Vector3.Zero, 1f);

        var expected = OrbitViewCamera.ComputeOffset(yaw, pitch, 1f);
        var actual = Vector3.Normalize(trackball.Position);
        Assert.True((actual - expected).Length < 1e-4f);
    }

    [Fact]
    public void Orbit_PitchGoesPastThePole()
    {
        var camera = new TrackballViewCamera();
        camera.SetAngles(0f, 0f);
        camera.Frame(Vector3.Zero, 1f);

        camera.Orbit(0f, 180f);

        var direction = Vector3.Normalize(camera.Position);
        Assert.True((direction - new Vector3(0f, 0f, -1f)).Length < 1e-3f);
        Assert.True(camera.Up.Y < -0.99f);
    }

    [Fact]
    public void Spin_KeepsDistance()
    {
        var camera = new TrackballViewCamera();
        camera.Frame(new Vector3(3f, 1f, 2f), 2f);
        float distance = camera.Distance;

        camera.Spin(Vector3.UnitY, 75f);
        camera.Orbit(33f, 210f);

        Assert.Equal(distance, (camera.Position - camera.Target).Length, 3);
    }
}
