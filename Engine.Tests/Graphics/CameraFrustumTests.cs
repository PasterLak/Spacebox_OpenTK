using Engine.Physics;
using OpenTK.Mathematics;

namespace Engine.Tests.Graphics;

public class CameraFrustumTests
{
    private static CameraFrustum LookingDownMinusZ()
    {
        var frustum = new CameraFrustum();
        frustum.SetFromMatrices(
            Matrix4.LookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY),
            Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90f), 1f, 0.1f, 100f));
        return frustum;
    }

    [Theory]
    [InlineData(-1f, -1f, -12f, 1f, 1f, -10f, true)]
    [InlineData(-1f, -1f, 10f, 1f, 1f, 12f, false)]
    [InlineData(30f, -1f, -12f, 32f, 1f, -10f, false)]
    [InlineData(8f, -1f, -12f, 14f, 1f, -10f, true)]
    [InlineData(-500f, -500f, -500f, 500f, 500f, 500f, true)]
    [InlineData(-1f, -1f, -150f, 1f, 1f, -120f, false)]
    public void Box_IsVisibleUnlessFullyOutsideOnePlane(float x0, float y0, float z0, float x1, float y1, float z1, bool expected)
    {
        var frustum = LookingDownMinusZ();
        var min = new Vector3(x0, y0, z0);
        var max = new Vector3(x1, y1, z1);

        Assert.Equal(expected, frustum.IsBoxInFrustum(min, max));
        Assert.Equal(expected, frustum.IsInFrustum(BoundingBox.CreateFromMinMax(min, max)));
    }

    [Theory]
    [InlineData(0f, 0f, -10f, 0f, true)]
    [InlineData(0f, 0f, 10f, 0f, false)]
    [InlineData(13.5f, 0f, -10f, -45f, true)]
    [InlineData(13.5f, 0f, -10f, 45f, false)]
    [InlineData(16.5f, 0f, -10f, 0f, false)]
    public void RotatedBox_UsesItsProjectedRadius(float x, float y, float z, float yawDegrees, bool expected)
    {
        var frustum = LookingDownMinusZ();
        var obb = new BoundingBoxOBB(new Vector3(x, y, z), new Vector3(10f, 2f, 2f), Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(yawDegrees)));

        Assert.Equal(expected, frustum.IsInFrustum(obb));
    }
}
