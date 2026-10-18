using Engine.Physics;
using OpenTK.Mathematics;

namespace Engine.Tests.Physics;

public class BoundingBoxTests
{
    [Fact]
    public void CreateFromMinMax_SetsCenterAndSize()
    {
        var box = BoundingBox.CreateFromMinMax(new Vector3(-2, 0, 4), new Vector3(2, 6, 8));

        Assert.Equal(new Vector3(0, 3, 6), box.Center);
        Assert.Equal(new Vector3(4, 6, 4), box.Size);
        Assert.Equal(new Vector3(-2, 0, 4), box.Min);
        Assert.Equal(new Vector3(2, 6, 8), box.Max);
        Assert.Equal(3f, box.GetLongestSide());
    }

    [Fact]
    public void CreateFromPoints_OrdersCorners()
    {
        var box = BoundingBox.CreateFromPoints(new Vector3(5, -1, 3), new Vector3(1, 2, -3));

        Assert.Equal(new Vector3(1, -1, -3), box.Min);
        Assert.Equal(new Vector3(5, 2, 3), box.Max);
    }

    [Theory]
    [InlineData(0, 0, 0, true)]
    [InlineData(1, 1, 1, true)]
    [InlineData(1.01f, 0, 0, false)]
    [InlineData(0, -1.5f, 0, false)]
    public void Contains_IncludesBorders(float x, float y, float z, bool expected)
    {
        var box = new BoundingBox(Vector3.Zero, new Vector3(2));

        Assert.Equal(expected, box.Contains(new Vector3(x, y, z)));
    }

    [Fact]
    public void Intersects_Boxes()
    {
        var box = new BoundingBox(Vector3.Zero, new Vector3(2));

        Assert.True(box.Intersects(new BoundingBox(new Vector3(1.5f, 0, 0), new Vector3(2))));
        Assert.True(box.Intersects(new BoundingBox(new Vector3(2, 0, 0), new Vector3(2))));
        Assert.False(box.Intersects(new BoundingBox(new Vector3(3, 0, 0), new Vector3(1.9f))));
    }

    [Fact]
    public void Intersects_Sphere()
    {
        var box = new BoundingBox(Vector3.Zero, new Vector3(2));

        Assert.True(box.Intersects(new BoundingSphere(new Vector3(2, 0, 0), 1.1f)));
        Assert.False(box.Intersects(new BoundingSphere(new Vector3(3, 3, 3), 1f)));
    }

    [Fact]
    public void Expand_CoversBothBoxes()
    {
        var box = BoundingBox.CreateFromMinMax(Vector3.Zero, Vector3.One);

        box.Expand(BoundingBox.CreateFromMinMax(new Vector3(-1, 2, 0), new Vector3(0, 3, 0.5f)));

        Assert.Equal(new Vector3(-1, 0, 0), box.Min);
        Assert.Equal(new Vector3(1, 3, 1), box.Max);
    }

    [Fact]
    public void Clone_IsIndependent()
    {
        var box = new BoundingBox(Vector3.One, Vector3.One);
        var clone = (BoundingBox)box.Clone();

        clone.Center = Vector3.Zero;

        Assert.Equal(Vector3.One, box.Center);
    }
}
