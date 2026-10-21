using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Wires;
using Spacebox.Game.Resource;

namespace Spacebox.Tests.Game;

public class WireShapeTests
{
    private static int Mask(params Direction[] directions)
    {
        int mask = 0;
        foreach (var direction in directions) mask |= WireShape.Bit(direction);
        return mask;
    }

    [Fact]
    public void FaceVectors_AreTheUnrotatedCube()
    {
        Assert.Equal(Vector3.UnitY, WireShape.FaceVectors[Direction.Up]);
        Assert.Equal(Vector3.UnitX, WireShape.FaceVectors[Direction.Right]);
        Assert.Equal(Vector3.UnitZ, WireShape.FaceVectors[Direction.Forward]);
    }

    [Fact]
    public void ConnectedFaces_ShowPorts()
    {
        var mask = Mask(Direction.Left, Direction.Right);
        Assert.Equal(WireTexture.Port, WireShape.Resolve(mask, Direction.Left, Direction.Up).Texture);
        Assert.Equal(WireTexture.Port, WireShape.Resolve(mask, Direction.Right, Direction.Up).Texture);
    }

    [Fact]
    public void StraightLine_TurnsItsRailsAlongTheLine()
    {
        var alongX = Mask(Direction.Left, Direction.Right);
        var front = WireShape.Resolve(alongX, Direction.Forward, Direction.Up);
        var top = WireShape.Resolve(alongX, Direction.Up, Direction.Up);

        Assert.Equal(new WireFace(WireTexture.Straight, 1), front);
        Assert.Equal(new WireFace(WireTexture.Straight, 1), top);

        var alongY = Mask(Direction.Up, Direction.Down);
        Assert.Equal(new WireFace(WireTexture.Straight, 0), WireShape.Resolve(alongY, Direction.Forward, Direction.Up));
    }

    [Fact]
    public void SingleConnection_ActsLikeALineTowardIt()
    {
        var mask = Mask(Direction.Forward);
        Assert.Equal(WireTexture.Port, WireShape.Resolve(mask, Direction.Back, Direction.Up).Texture);
        Assert.Equal(WireTexture.Straight, WireShape.Resolve(mask, Direction.Up, Direction.Up).Texture);
    }

    [Fact]
    public void NoConnection_UsesThePlacementAxis()
    {
        Assert.Equal(WireTexture.Port, WireShape.Resolve(0, Direction.Right, Direction.Left).Texture);
        Assert.Equal(new WireFace(WireTexture.Straight, 1), WireShape.Resolve(0, Direction.Forward, Direction.Left));
    }

    [Fact]
    public void PerpendicularPair_IsAnAngle()
    {
        var mask = Mask(Direction.Up, Direction.Right);
        var front = WireShape.Resolve(mask, Direction.Forward, Direction.Up);
        var back = WireShape.Resolve(mask, Direction.Back, Direction.Up);

        Assert.Equal(new WireFace(WireTexture.Angle, 0), front);
        Assert.Equal(WireTexture.Angle, back.Texture);
        Assert.NotEqual(front.QuarterTurns, back.QuarterTurns);

        Assert.Equal(new WireFace(WireTexture.Straight, 1), WireShape.Resolve(mask, Direction.Down, Direction.Up));
        Assert.Equal(new WireFace(WireTexture.Straight, 0), WireShape.Resolve(mask, Direction.Left, Direction.Up));
    }

    [Fact]
    public void ThreeOrMoreConnections_AreAHub()
    {
        var mask = Mask(Direction.Up, Direction.Left, Direction.Right);
        Assert.Equal(WireTexture.Hub, WireShape.Resolve(mask, Direction.Forward, Direction.Up).Texture);
        Assert.Equal(WireTexture.Port, WireShape.Resolve(mask, Direction.Up, Direction.Up).Texture);
        Assert.Equal(WireTexture.Hub, WireShape.Resolve(mask, Direction.Down, Direction.Up).Texture);
    }

    [Fact]
    public void Rotate_ShiftsCornersClockwise()
    {
        var uvs = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        var turned = CableBlockData.Rotate(uvs, 1);
        Assert.Equal(new[] { uvs[1], uvs[2], uvs[3], uvs[0] }, turned);
        Assert.Equal(uvs, CableBlockData.Rotate(uvs, 4 % 4));
    }
}
