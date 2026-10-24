using OpenTK.Mathematics;
using Spacebox.Game.Player.Building;

namespace Spacebox.Tests.Game;

public class PlayerAlignedFrameTests
{
    private static void Near(Vector3 expected, Vector3 actual)
    {
        Assert.True((expected - actual).Length < 1e-4f, $"expected {expected}, got {actual}");
    }

    [Fact]
    public void DefaultView_IsIdentity()
    {
        var q = PlayerAlignedFrame.FromView(-Vector3.UnitZ, Vector3.UnitY);
        Near(Vector3.UnitX, Vector3.Transform(Vector3.UnitX, q));
        Near(Vector3.UnitY, Vector3.Transform(Vector3.UnitY, q));
    }

    [Fact]
    public void RotatedView_MapsLocalAxesToThePlayerAxes()
    {
        var front = Vector3.Normalize(new Vector3(1f, 0.3f, -0.4f));
        var roughUp = Vector3.Normalize(new Vector3(0.2f, 1f, 0.5f));
        var q = PlayerAlignedFrame.FromView(front, roughUp);

        var right = Vector3.Normalize(Vector3.Cross(front, roughUp));
        var up = Vector3.Cross(right, front);
        Near(right, Vector3.Transform(Vector3.UnitX, q));
        Near(up, Vector3.Transform(Vector3.UnitY, q));
        Near(front, Vector3.Transform(-Vector3.UnitZ, q));
    }

    [Fact]
    public void UpsideDownView_KeepsThePlayerUp()
    {
        var q = PlayerAlignedFrame.FromView(-Vector3.UnitZ, -Vector3.UnitY);
        Near(-Vector3.UnitY, Vector3.Transform(Vector3.UnitY, q));
        Near(-Vector3.UnitX, Vector3.Transform(Vector3.UnitX, q));
    }

    [Fact]
    public void CornerForCenter_PutsTheBlockCenterOnTheTarget()
    {
        var q = PlayerAlignedFrame.FromView(Vector3.Normalize(new Vector3(1, 0, -1)), Vector3.UnitY);
        var center = new Vector3(10, 20, 30);
        var corner = PlayerAlignedFrame.CornerForCenter(center, q);
        Near(center, corner + Vector3.Transform(new Vector3(0.5f), q));
    }

    [Fact]
    public void Sanitize_RejectsBrokenValues()
    {
        Assert.Equal(Quaternion.Identity, PlayerAlignedFrame.Sanitize(new Quaternion(float.NaN, 0, 0, 1)));
        Assert.Equal(Quaternion.Identity, PlayerAlignedFrame.Sanitize(new Quaternion(0, 0, 0, 0)));
        Assert.Equal(Quaternion.Identity, PlayerAlignedFrame.FromView(Vector3.UnitY, Vector3.UnitY));
    }
}
