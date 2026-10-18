using Engine.Physics;
using OpenTK.Mathematics;

namespace Engine.Tests.Physics;

public class BoundingVolumeTests
{
    [Fact]
    public void Spheres_IntersectWhenTouching()
    {
        var a = new BoundingSphere(Vector3.Zero, 1);

        Assert.True(a.Intersects(new BoundingSphere(new Vector3(2, 0, 0), 1)));
        Assert.False(a.Intersects(new BoundingSphere(new Vector3(2.01f, 0, 0), 1)));
    }

    [Fact]
    public void SphereAndBox_IntersectBothWays()
    {
        var sphere = new BoundingSphere(new Vector3(2, 0, 0), 1.1f);
        var box = new BoundingBox(Vector3.Zero, new Vector3(2));

        Assert.True(sphere.Intersects(box));
        Assert.True(box.Intersects(sphere));
    }

    [Fact]
    public void Obb_GetCorners_RotatesAroundCenter()
    {
        var obb = new BoundingBoxOBB(new Vector3(5, 0, 0), new Vector3(2), Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.PiOver4));

        var corners = obb.GetCorners();

        Assert.Equal(8, corners.Length);
        foreach (var corner in corners)
            Assert.Equal(MathF.Sqrt(3), Vector3.Distance(corner, obb.Center), 3);
        Assert.Contains(corners, c => MathF.Abs(c.X - (5 + MathF.Sqrt(2))) < 0.001f);
    }

    [Fact]
    public void Obb_IntersectsAabbAndSphere()
    {
        var obb = new BoundingBoxOBB(Vector3.Zero, new Vector3(2), Quaternion.FromAxisAngle(Vector3.UnitZ, MathHelper.PiOver4));

        Assert.True(obb.Intersects(new BoundingBox(new Vector3(2.2f, 0, 0), new Vector3(2))));
        Assert.False(obb.Intersects(new BoundingBox(new Vector3(2.6f, 0, 0), new Vector3(2))));
        Assert.True(obb.Intersects(new BoundingSphere(new Vector3(1.5f, 0, 0), 0.2f)));
        Assert.False(obb.Intersects(new BoundingSphere(new Vector3(3, 3, 0), 0.5f)));
    }

    [Fact]
    public void Clones_AreIndependentCopies()
    {
        var sphere = new BoundingSphere(Vector3.One, 2);
        var obb = new BoundingBoxOBB(Vector3.One, Vector3.One, Quaternion.Identity);

        var sphereClone = (BoundingSphere)sphere.Clone();
        var obbClone = (BoundingBoxOBB)obb.Clone();
        sphereClone.Radius = 5;
        obbClone.Center = Vector3.Zero;

        Assert.Equal(2, sphere.Radius);
        Assert.Equal(Vector3.One, obb.Center);
    }

    [Fact]
    public void IsBehindCamera_UsesForwardDirection()
    {
        Assert.True(CameraFrustum.IsBehindCamera(new Vector3(0, 0, 5), Vector3.Zero, -Vector3.UnitZ));
        Assert.False(CameraFrustum.IsBehindCamera(new Vector3(0, 0, -5), Vector3.Zero, -Vector3.UnitZ));
    }
}
