using OpenTK.Mathematics;
using Spacebox.Game.Physics;

namespace Spacebox.Tests;

public class SphereBoxMathTests
{
    [Fact]
    public void TryGetContact_FaceContactPushesAlongFaceNormal()
    {
        bool hit = SphereBoxMath.TryGetContact(new Vector3(1.3f, 0.5f, 0.5f), 0.4f, Vector3.Zero, Vector3.One, out var normal, out float depth);

        Assert.True(hit);
        Assert.Equal(Vector3.UnitX, normal);
        Assert.Equal(0.1f, depth, 4);
    }

    [Fact]
    public void TryGetContact_EdgeContactPushesDiagonally()
    {
        bool hit = SphereBoxMath.TryGetContact(new Vector3(1.2f, 1.2f, 0.5f), 0.4f, Vector3.Zero, Vector3.One, out var normal, out _);

        Assert.True(hit);
        Assert.Equal(MathF.Sqrt(0.5f), normal.X, 4);
        Assert.Equal(MathF.Sqrt(0.5f), normal.Y, 4);
        Assert.Equal(0f, normal.Z, 4);
    }

    [Fact]
    public void TryGetContact_SeparatedSphereHasNoContact()
    {
        Assert.False(SphereBoxMath.TryGetContact(new Vector3(1.5f, 0.5f, 0.5f), 0.4f, Vector3.Zero, Vector3.One, out _, out _));
    }

    [Fact]
    public void TryGetContact_CenterInsideUsesNearestFace()
    {
        bool hit = SphereBoxMath.TryGetContact(new Vector3(0.5f, 0.9f, 0.5f), 0.4f, Vector3.Zero, Vector3.One, out var normal, out float depth);

        Assert.True(hit);
        Assert.Equal(Vector3.UnitY, normal);
        Assert.Equal(0.5f, depth, 4);
    }

    [Fact]
    public void RemoveInward_KeepsTangentialMotion()
    {
        var result = SphereBoxMath.RemoveInward(new Vector3(-3f, 0f, 2f), Vector3.UnitX, out float inward);

        Assert.Equal(new Vector3(0f, 0f, 2f), result);
        Assert.Equal(3f, inward);
    }

    [Fact]
    public void RemoveInward_IgnoresOutwardMotion()
    {
        var velocity = new Vector3(1f, 2f, 0f);
        var result = SphereBoxMath.RemoveInward(velocity, Vector3.UnitX, out float inward);

        Assert.Equal(velocity, result);
        Assert.Equal(0f, inward);
    }
}
