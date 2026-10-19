using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Physics;

namespace Spacebox.Tests;

public class BoxMathTests
{
    private const float Skin = EntityBoxCollider.Skin;

    [Fact]
    public void ClipDistance_StopsBeforeBoxAhead()
    {
        float allowed = BoxMath.ClipDistance(0f, 1f, 1.5f, 2f, 2f, true, Skin);
        Assert.Equal(0.5f - Skin, allowed, 5);
    }

    [Fact]
    public void ClipDistance_IgnoresBoxBehindOrOverlapping()
    {
        Assert.Equal(2f, BoxMath.ClipDistance(0f, 1f, 0.5f, 2f, 2f, true, Skin));
        Assert.Equal(2f, BoxMath.ClipDistance(0f, 1f, -2f, -1f, 2f, true, Skin));
    }

    [Fact]
    public void ClipDistance_NegativeStopsAboveBox()
    {
        float allowed = BoxMath.ClipDistance(1f, 2f, 0f, 0.8f, -1f, false, Skin);
        Assert.Equal(-(0.2f - Skin), allowed, 5);
    }

    [Fact]
    public void ClipDistance_KeepsZeroWhenAlreadyBlocked()
    {
        Assert.Equal(0f, BoxMath.ClipDistance(0f, 1f, 1.5f, 2f, 0f, true, Skin));
    }

    [Theory]
    [InlineData(0.3f, 0.7f, 0f, 0.29f, false)]
    [InlineData(0.3f, 0.7f, 0f, 0.4f, true)]
    [InlineData(0.3f, 0.7f, 0.69f, 2f, true)]
    [InlineData(0.3f, 0.7f, 0.7f, 2f, false)]
    public void OverlapsOnAxis_UsesSkin(float boxMin, float boxMax, float moverMin, float moverMax, bool expected)
    {
        Assert.Equal(expected, BoxMath.OverlapsOnAxis(boxMin, boxMax, moverMin, moverMax, Skin));
    }

    [Fact]
    public void RayIntersects_HitsSideOfThinBox()
    {
        bool hit = BoxMath.RayIntersects(new Vector3(-1f, 0.5f, 0.5f), Vector3.UnitX,
            new Vector3(0.3f, 0f, 0.3f), new Vector3(0.7f, 0.8f, 0.7f), out float distance, out var normal);

        Assert.True(hit);
        Assert.Equal(1.3f, distance, 5);
        Assert.Equal(new Vector3SByte(-1, 0, 0), normal);
    }

    [Fact]
    public void RayIntersects_MissesEmptyPartOfCell()
    {
        bool hit = BoxMath.RayIntersects(new Vector3(-1f, 0.9f, 0.5f), Vector3.UnitX,
            new Vector3(0.3f, 0f, 0.3f), new Vector3(0.7f, 0.8f, 0.7f), out _, out _);

        Assert.False(hit);
    }

    [Fact]
    public void RayIntersects_FromAboveReturnsUpNormal()
    {
        bool hit = BoxMath.RayIntersects(new Vector3(0.5f, 3f, 0.5f), -Vector3.UnitY,
            new Vector3(0.3f, 0f, 0.3f), new Vector3(0.7f, 0.8f, 0.7f), out float distance, out var normal);

        Assert.True(hit);
        Assert.Equal(2.2f, distance, 5);
        Assert.Equal(new Vector3SByte(0, 1, 0), normal);
    }

    [Fact]
    public void RayIntersects_BoxBehindOriginIsMissed()
    {
        bool hit = BoxMath.RayIntersects(new Vector3(2f, 0.5f, 0.5f), Vector3.UnitX,
            new Vector3(0.3f, 0f, 0.3f), new Vector3(0.7f, 0.8f, 0.7f), out _, out _);

        Assert.False(hit);
    }

    [Fact]
    public void RayIntersects_OriginInsideReturnsNearestFace()
    {
        bool hit = BoxMath.RayIntersects(new Vector3(0.5f, 0.75f, 0.5f), Vector3.UnitX,
            new Vector3(0.3f, 0f, 0.3f), new Vector3(0.7f, 0.8f, 0.7f), out float distance, out var normal);

        Assert.True(hit);
        Assert.Equal(0f, distance);
        Assert.Equal(new Vector3SByte(0, 1, 0), normal);
    }
}
