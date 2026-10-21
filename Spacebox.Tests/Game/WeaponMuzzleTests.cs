using Engine;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Effects;
using Spacebox.Game.Player;

namespace Spacebox.Tests.Game;

public class WeaponMuzzleTests
{
    private static Color4[,] Sprite(int width, int height, params (int X, int Y)[] opaque)
    {
        var pixels = new Color4[width, height];
        foreach (var (x, y) in opaque) pixels[x, y] = new Color4(1f, 1f, 1f, 1f);
        return pixels;
    }

    [Fact]
    public void Muzzle_FindsBothEndsOfTheSprite()
    {
        var pixels = Sprite(16, 16, (2, 4), (3, 4), (10, 6), (10, 8), (12, 7));

        Assert.True(ItemMuzzle.TryFromPixels(pixels, out var muzzle));
        Assert.Equal(new Vector2(2f, 4.5f), muzzle.LowEnd);
        Assert.Equal(new Vector2(13f, 7.5f), muzzle.HighEnd);
        Assert.Equal(16, muzzle.Width);
    }

    [Fact]
    public void Muzzle_EmptySprite_HasNone()
    {
        Assert.False(ItemMuzzle.TryFromPixels(new Color4[8, 8], out _));
        Assert.False(ItemMuzzle.TryFromPixels(null, out _));
    }

    [Fact]
    public void Muzzle_PicksTheEndInFrontOfTheShooter()
    {
        var front = new Vector3(0, 0, 3);
        var back = new Vector3(0, 0, -1);

        Assert.Equal(front, ItemMuzzle.PickForward(back, front, Vector3.Zero, Vector3.UnitZ));
        Assert.Equal(back, ItemMuzzle.PickForward(back, front, Vector3.Zero, -Vector3.UnitZ));
    }

    [Fact]
    public void ViewModelPoint_CenterMapsAlongTheCameraFront()
    {
        var projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(80f), 16f / 9f, 0.01f, 100f);

        Assert.True(ViewModelPoint.TryToWorld(new Vector3(0, 0, -0.5f), projection, new Vector3(10, 0, 0), Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitY, 70f, 16f / 9f, out var world));
        Assert.Equal(10.5f, world.X, 3);
        Assert.Equal(0f, world.Y, 3);
        Assert.Equal(0f, world.Z, 3);
        Assert.False(ViewModelPoint.TryToWorld(new Vector3(0, 0, 0.5f), projection, Vector3.Zero, -Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, 70f, 1f, out _));
    }

    [Fact]
    public void ViewModelPoint_KeepsTheScreenSide()
    {
        var projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(80f), 1f, 0.01f, 100f);

        ViewModelPoint.TryToWorld(new Vector3(0.1f, -0.1f, -0.4f), projection, Vector3.Zero, -Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, 80f, 1f, out var world);

        Assert.True(world.X > 0f);
        Assert.True(world.Y < 0f);
        Assert.True(world.Z < 0f);
    }

    [Fact]
    public void VisualOffset_FadesOutAfterTheBlendTime()
    {
        Assert.Equal(1f, Projectile.VisualOffsetFactor(Projectile.VisualBlendTime), 3);
        Assert.Equal(0.5f, Projectile.VisualOffsetFactor(Projectile.VisualBlendTime * 0.5f), 3);
        Assert.Equal(0f, Projectile.VisualOffsetFactor(-1f), 3);
    }

    [Fact]
    public void Jetpack_NozzlesAreBehindAndBelowTheHead()
    {
        var left = JetpackNozzles.Left(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY, Vector3.UnitX);
        var right = JetpackNozzles.Right(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY, Vector3.UnitX);
        var exhaust = JetpackNozzles.ExhaustDirection(-Vector3.UnitZ, Vector3.UnitY);

        Assert.True(left.Z > 0f && left.Y < 0f && left.X < 0f);
        Assert.True(right.X > 0f);
        Assert.True(exhaust.Z > 0f && exhaust.Y < 0f);
        Assert.Equal(1f, exhaust.Length, 3);
    }
}
