using Engine.Physics;
using OpenTK.Mathematics;

namespace Engine.Tests.Physics;

public class RayTests
{
    private const int Digits = 4;

    [Fact]
    public void Constructor_NormalizesDirection()
    {
        var ray = new Ray(Vector3.Zero, new Vector3(0, 0, 5), 10);

        Assert.Equal(Vector3.UnitZ, ray.Direction);
        Assert.Equal(new Vector3(0, 0, 3), ray.GetPoint(3));
    }

    [Fact]
    public void Sphere_HitReturnsDistanceToSurface()
    {
        var ray = new Ray(Vector3.Zero, Vector3.UnitX, 100);

        Assert.True(ray.Intersects(new BoundingSphere(new Vector3(10, 0, 0), 2), out float distance));
        Assert.Equal(8f, distance, Digits);
    }

    [Fact]
    public void Sphere_FromInside_ReturnsExitDistance()
    {
        var ray = new Ray(Vector3.Zero, Vector3.UnitX, 100);

        Assert.True(ray.IntersectsSphere(Vector3.Zero, 3, out float distance));
        Assert.Equal(3f, distance, Digits);
    }

    [Theory]
    [InlineData(10, 5, 0, 2, 100)]
    [InlineData(-10, 0, 0, 2, 100)]
    [InlineData(10, 0, 0, 2, 5)]
    public void Sphere_MissBesideBehindOrTooFar(float x, float y, float z, float radius, float length)
    {
        var ray = new Ray(Vector3.Zero, Vector3.UnitX, length);

        Assert.False(ray.IntersectsSphere(new Vector3(x, y, z), radius, out _));
    }

    [Fact]
    public void Box_HitReturnsEntryDistance()
    {
        var ray = new Ray(new Vector3(0, 0.5f, 0.5f), Vector3.UnitX, 100);
        var box = BoundingBox.CreateFromMinMax(new Vector3(5, 0, 0), new Vector3(6, 1, 1));

        Assert.True(ray.Intersects(box, out float distance));
        Assert.Equal(5f, distance, Digits);
    }

    [Fact]
    public void Box_FromInside_HitsAtOrigin()
    {
        var ray = new Ray(Vector3.Zero, Vector3.UnitY, 100);
        var box = new BoundingBox(Vector3.Zero, new Vector3(4));

        Assert.True(ray.Intersects(box, out float distance));
        Assert.Equal(0f, distance, Digits);
    }

    [Fact]
    public void Box_DiagonalHit()
    {
        var ray = new Ray(Vector3.Zero, Vector3.One, 100);
        var box = BoundingBox.CreateFromMinMax(new Vector3(3), new Vector3(4));

        Assert.True(ray.Intersects(box, out float distance));
        Assert.Equal(MathF.Sqrt(27), distance, Digits);
    }

    [Theory]
    [InlineData(0, 2, 0)]
    [InlineData(-1, 0.5f, 0.5f)]
    public void Box_MissParallelOrBehind(float dx, float oy, float oz)
    {
        var box = BoundingBox.CreateFromMinMax(new Vector3(5, 0, 0), new Vector3(6, 1, 1));
        var direction = dx == 0 ? Vector3.UnitX : new Vector3(dx, 0, 0);
        var ray = new Ray(new Vector3(0, oy, oz), direction, 100);

        Assert.False(ray.Intersects(box, out _));
    }

    [Fact]
    public void Box_BeyondLength_Misses()
    {
        var ray = new Ray(new Vector3(0, 0.5f, 0.5f), Vector3.UnitX, 4);

        Assert.False(ray.Intersects(BoundingBox.CreateFromMinMax(new Vector3(5, 0, 0), new Vector3(6, 1, 1)), out _));
    }

    [Fact]
    public void RotatedBox_UsesItsOrientation()
    {
        var rotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(45));
        var obb = new BoundingBoxOBB(new Vector3(10, 0, 0), new Vector3(2, 2, 2), rotation);

        Assert.True(new Ray(Vector3.Zero, Vector3.UnitX, 100).Intersects(obb, out float distance));
        Assert.Equal(10f - MathF.Sqrt(2), distance, 3);

        Assert.False(new Ray(new Vector3(0, 0, 1.6f), Vector3.UnitX, 100).Intersects(obb, out _));
    }

    [Fact]
    public void IntersectsVolume_DispatchesByType()
    {
        var ray = new Ray(Vector3.Zero, Vector3.UnitX, 100);

        Assert.True(ray.Intersects((BoundingVolume)new BoundingSphere(new Vector3(5, 0, 0), 1), out var sphereDistance));
        Assert.True(ray.Intersects((BoundingVolume)new BoundingBox(new Vector3(5, 0, 0), Vector3.One), out var boxDistance));
        Assert.Equal(4f, sphereDistance, Digits);
        Assert.Equal(4.5f, boxDistance, Digits);
    }

    [Fact]
    public void Ricochet_ReflectsAroundNormal()
    {
        var ray = new Ray(Vector3.Zero, new Vector3(1, -1, 0), 10);

        var bounced = ray.CalculateRicochetRay(new Vector3(1, -1, 0), Vector3.UnitY, 5);

        Assert.Equal(new Vector3(1, 1, 0).Normalized().X, bounced.Direction.X, Digits);
        Assert.Equal(new Vector3(1, 1, 0).Normalized().Y, bounced.Direction.Y, Digits);
        Assert.Equal(5f, bounced.Length);
        Assert.True(bounced.Origin.Y > -1f);
    }

    [Fact]
    public void Ricochet_WithBlockNormal_MatchesVectorNormal()
    {
        var ray = new Ray(Vector3.Zero, new Vector3(1, -2, 0.5f), 10);

        var fromBlock = Ray.CalculateRicochet(ray, Vector3.Zero, new Vector3SByte(0, 1, 0));
        var fromVector = ray.CalculateRicochetRay(Vector3.Zero, Vector3.UnitY, 1).Direction;

        Assert.Equal(fromVector.X, fromBlock.X, Digits);
        Assert.Equal(fromVector.Y, fromBlock.Y, Digits);
        Assert.Equal(fromVector.Z, fromBlock.Z, Digits);
    }

    [Theory]
    [InlineData(0, -1, 0, 90)]
    [InlineData(1, -1, 0, 45)]
    [InlineData(1, 0, 0, 0)]
    public void IncidentAngle_IsMeasuredFromSurface(float x, float y, float z, float expected)
    {
        var ray = new Ray(Vector3.Zero, new Vector3(x, y, z), 10);

        Assert.Equal(expected, Ray.CalculateIncidentAngle(ray, Vector3.UnitY), 3);
        Assert.Equal(expected, Ray.CalculateIncidentAngle(ray, new Vector3SByte(0, 1, 0)), 3);
    }
}
