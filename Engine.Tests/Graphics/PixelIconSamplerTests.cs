using Engine.Graphics.Icons;
using OpenTK.Mathematics;

namespace Engine.Tests.Graphics;

public class PixelIconSamplerTests
{
    private static readonly Color4 Top = new Color4(1f, 0f, 0f, 1f);
    private static readonly Color4 Bottom = new Color4(0f, 0f, 1f, 1f);

    private static Color4[,] TwoRows()
    {
        var pixels = new Color4[2, 2];
        pixels[0, 0] = Top;
        pixels[1, 0] = Top;
        pixels[0, 1] = Bottom;
        pixels[1, 1] = Bottom;
        return pixels;
    }

    [Fact]
    public void GlUvs_HaveTheImageTopAtVOne()
    {
        var sample = PixelIconSampler.FromTopDown(TwoRows());

        Assert.True(sample(new Vector2(0.5f, 0.9f), Vector2.Zero, out var high));
        Assert.True(sample(new Vector2(0.5f, 0.1f), Vector2.Zero, out var low));
        Assert.Equal(Top, high);
        Assert.Equal(Bottom, low);
    }

    [Fact]
    public void TopOriginUvs_HaveTheImageTopAtVZero()
    {
        var sample = PixelIconSampler.FromTopDown(TwoRows(), true);

        Assert.True(sample(new Vector2(0.5f, 0.1f), Vector2.Zero, out var color));
        Assert.Equal(Top, color);
    }

    [Fact]
    public void FitToSize_EnlargesSmallModels()
    {
        var vertices = new List<float>();
        var indices = new List<uint>();
        var corners = new[] { new Vector3(0, 0.2f, 0), new Vector3(0.2f, 0.2f, 0), new Vector3(0.2f, 0.2f, 0.2f), new Vector3(0, 0.2f, 0.2f) };
        foreach (var p in corners) vertices.AddRange(new[] { p.X, p.Y, p.Z, 0f, 1f, 0f, 0.5f, 0.5f });
        indices.AddRange(new uint[] { 0, 1, 2, 2, 3, 0 });
        IconSampler white = (Vector2 uv, Vector2 anchor, out Color4 color) => { color = Color4.White; return true; };

        int normal = Filled(IsometricRasterizer.Render(vertices.ToArray(), indices.ToArray(), 64, white));
        int fitted = Filled(IsometricRasterizer.Render(vertices.ToArray(), indices.ToArray(), 64, white, true));

        Assert.True(fitted > normal * 4, $"fitted {fitted}, normal {normal}");
    }

    private static int Filled(Color4[,] pixels)
    {
        int n = 0;
        foreach (var p in pixels) if (p.A > 0f) n++;
        return n;
    }
}
