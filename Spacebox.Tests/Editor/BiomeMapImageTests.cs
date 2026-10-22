using OpenTK.Mathematics;
using Spacebox.Editor.Generation;

namespace Spacebox.Tests.Editor;

public class BiomeMapImageTests
{
    [Fact]
    public void BuildSlice_CallsEveryPixel()
    {
        var pixels = BiomeMapImage.BuildSlice(3, 2, (x, y) => new Color4(x, y, 0, 1));

        Assert.Equal(3, pixels.GetLength(0));
        Assert.Equal(2, pixels.GetLength(1));
        Assert.Equal(2f, pixels[2, 1].R);
        Assert.Equal(1f, pixels[2, 1].G);
    }

    [Fact]
    public void ToSectorLocal_KeepsLocalAndShiftsWorldPoints()
    {
        var origin = new Vector3(8192, 0, 0);

        Assert.Equal(new Vector3(10, 20, 30), BiomeMapImage.ToSectorLocal(new Vector3(10, 20, 30), origin, 8192));
        Assert.Equal(new Vector3(10, 20, 30), BiomeMapImage.ToSectorLocal(new Vector3(8202, 20, 30), origin, 8192));
    }

    [Fact]
    public void ToImage_ScalesXZ()
    {
        var p = BiomeMapImage.ToImage(new Vector3(4096, 0, 2048), 8192, new Vector2(100, 100));

        Assert.Equal(new Vector2(50, 25), p);
    }

    [Fact]
    public void IsInSlice_UsesBand()
    {
        Assert.True(BiomeMapImage.IsInSlice(64 * 64, 64, 128, 8192, 2));
        Assert.False(BiomeMapImage.IsInSlice(0, 64, 128, 8192, 2));
    }

    [Fact]
    public void RingRadius_IsTheCircleOfTheDistanceInTheSlice()
    {
        Assert.Equal(400f, BiomeMapImage.RingRadius(500f, 300f), 3);
        Assert.Equal(0f, BiomeMapImage.RingRadius(200f, 300f));
    }
}
