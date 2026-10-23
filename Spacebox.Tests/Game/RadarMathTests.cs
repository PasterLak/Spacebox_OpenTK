using OpenTK.Mathematics;
using Spacebox.Game.Radar;

namespace Spacebox.Tests.Game;

public class RadarMathTests
{
    [Fact]
    public void Project_PutsForwardAtTheTopAndRightOnTheRight()
    {
        var right = Vector3.UnitX;
        var front = -Vector3.UnitZ;

        Assert.Equal(new Vector2(0f, -1f), RadarMath.Project(new Vector3(0f, 0f, -10f), right, front));
        Assert.Equal(new Vector2(1f, 0f), RadarMath.Project(new Vector3(10f, 0f, 0f), right, front));
        Assert.Equal(Vector2.Zero, RadarMath.Project(new Vector3(0f, 10f, 0f), right, front));
    }

    [Theory]
    [InlineData(0f, -1f)]
    [InlineData(1f, 0f)]
    [InlineData(-0.6f, 0.8f)]
    public void SweepAngle_PointsTheLineAtTheContact(float x, float y)
    {
        var direction = new Vector2(x, y);
        var line = RadarMath.DirectionOf(RadarMath.SweepAngleOf(direction));

        Assert.Equal(direction.X, line.X, 4);
        Assert.Equal(direction.Y, line.Y, 4);
    }

    [Fact]
    public void Crossed_HandlesTheWrapAround()
    {
        Assert.True(RadarMath.Crossed(1f, 1.2f, 1.1f));
        Assert.False(RadarMath.Crossed(1f, 1.2f, 1.3f));
        Assert.True(RadarMath.Crossed(6.2f, 0.1f, 0.05f));
        Assert.True(RadarMath.Crossed(6.2f, 0.1f, 6.25f));
        Assert.False(RadarMath.Crossed(6.2f, 0.1f, 3f));
    }

    [Fact]
    public void Radius_StaysInsideTheDisplay()
    {
        Assert.Equal(0.1f, RadarMath.Radius(0f, 500f), 4);
        Assert.Equal(0.85f, RadarMath.Radius(5000f, 500f), 4);
    }

    [Fact]
    public void Textures_ClipOutsideTheCircle()
    {
        Assert.True(RadarTextures.IsOutside(0, 0, 64, 64));
        Assert.False(RadarTextures.IsOutside(32, 32, 64, 64));
        Assert.True(RadarTextures.IsRingColor(new Color4(0.1f, 0.6f, 0.1f, 1f)));
        Assert.False(RadarTextures.IsRingColor(new Color4(0.5f, 0.5f, 0.5f, 1f)));
    }
}
