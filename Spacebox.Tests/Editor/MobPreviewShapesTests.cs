using OpenTK.Mathematics;
using Spacebox.Editor.Preview;

namespace Spacebox.Tests.Editor;

public class MobPreviewShapesTests
{
    [Fact]
    public void Circle_IsClosedAndOnRadius()
    {
        var points = MobPreviewShapes.Circle(2f, 0.5f, 16);

        Assert.Equal(17, points.Count);
        Assert.True((points[0] - points[^1]).Length < 1e-4f);
        foreach (var p in points)
        {
            Assert.Equal(2f, new Vector2(p.X, p.Z).Length, 3);
            Assert.Equal(0.5f, p.Y);
        }
    }

    [Fact]
    public void Circle_ClampsSegments()
    {
        Assert.Equal(4, MobPreviewShapes.Circle(1f, 0f, 1).Count);
    }

    [Fact]
    public void CubeEdges_StayInsideCube()
    {
        var min = new Vector3(1f, 2f, 3f);
        var points = MobPreviewShapes.CubeEdges(min, 1f);

        Assert.All(points, p =>
        {
            Assert.InRange(p.X, 1f, 2f);
            Assert.InRange(p.Y, 2f, 3f);
            Assert.InRange(p.Z, 3f, 4f);
        });
        Assert.Contains(min, points);
        Assert.Contains(min + Vector3.One, points);
    }
}
