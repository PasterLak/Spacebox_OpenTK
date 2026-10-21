using Engine.Graphics.Icons;
using OpenTK.Mathematics;

namespace Engine.Tests.Graphics;

public class IsometricRasterizerTests
{
    private static readonly Color4 Red = new Color4(1f, 0f, 0f, 1f);
    private static readonly Color4 Blue = new Color4(0f, 0f, 1f, 1f);

    private static void Quad(List<float> v, List<uint> i, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, float uvX)
    {
        uint start = (uint)(v.Count / IsometricRasterizer.FloatsPerVertex);
        foreach (var p in new[] { a, b, c, d })
            v.AddRange(new[] { p.X, p.Y, p.Z, normal.X, normal.Y, normal.Z, uvX, 0.5f });
        i.AddRange(new[] { start, start + 1, start + 2, start + 2, start + 3, start });
    }

    private static bool Sample(Vector2 uv, Vector2 anchor, out Color4 color)
    {
        color = anchor.X < 0.5f ? Red : Blue;
        return true;
    }

    private static int Count(Color4[,] pixels)
    {
        int n = 0;
        foreach (var p in pixels) if (p.A > 0f) n++;
        return n;
    }

    [Fact]
    public void UnitCubeTop_FillsTheUpperRhombus()
    {
        var v = new List<float>();
        var i = new List<uint>();
        Quad(v, i, new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(0, 1, 1), Vector3.UnitY, 0.25f);

        var pixels = IsometricRasterizer.Render(v.ToArray(), i.ToArray(), 64, Sample);

        Assert.InRange(Count(pixels), 64 * 32 / 2 - 80, 64 * 32 / 2 + 80);
        Assert.True(pixels[32, 31].A > 0f);
        Assert.Equal(0f, pixels[2, 2].A);
    }

    [Fact]
    public void NearerTriangle_WinsTheDepthTest()
    {
        var v = new List<float>();
        var i = new List<uint>();
        Quad(v, i, new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1), Vector3.UnitZ, 0.75f);
        Quad(v, i, new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), Vector3.UnitZ, 0.25f);

        var pixels = IsometricRasterizer.Render(v.ToArray(), i.ToArray(), 64, Sample);

        int blue = 0, red = 0;
        foreach (var p in pixels)
        {
            if (p.A == 0f) continue;
            if (p.B > p.R) blue++; else red++;
        }
        Assert.True(blue > red);
    }

    [Fact]
    public void LargeModels_AreScaledToFit()
    {
        var v = new List<float>();
        var i = new List<uint>();
        Quad(v, i, new Vector3(0, 4, 0), new Vector3(4, 4, 0), new Vector3(4, 4, 4), new Vector3(0, 4, 4), Vector3.UnitY, 0.25f);

        var pixels = IsometricRasterizer.Render(v.ToArray(), i.ToArray(), 64, Sample);

        Assert.True(pixels[0, 32].A > 0f || pixels[1, 32].A > 0f);
        Assert.True(pixels[63, 32].A > 0f || pixels[62, 32].A > 0f);
    }

    [Fact]
    public void Shade_LightsTopsAndDarkensTheLeftSide()
    {
        Assert.Equal(IsometricRasterizer.TopLight, IsometricRasterizer.Shade(Vector3.UnitY));
        Assert.Equal(-IsometricRasterizer.SideShadow, IsometricRasterizer.Shade(-Vector3.UnitX));
        Assert.Equal(0f, IsometricRasterizer.Shade(Vector3.UnitZ));
    }
}
