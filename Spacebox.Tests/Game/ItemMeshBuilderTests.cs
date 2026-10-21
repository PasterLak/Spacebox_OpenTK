using OpenTK.Mathematics;
using Spacebox.Game;

namespace Spacebox.Tests.Game;

public class ItemMeshBuilderTests
{
    private static bool[,] Mask(params string[] rows)
    {
        int height = rows.Length;
        int width = rows[0].Length;
        var mask = new bool[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                mask[x, y] = rows[y][x] == '#';
        return mask;
    }

    private static int Quads((float[] vertices, uint[] indices) mesh) => mesh.indices.Length / 6;

    [Fact]
    public void FullSquare_IsOneFrontQuadWithMergedSides()
    {
        var mask = Mask("####", "####", "####", "####");

        Assert.Equal(6, Quads(ItemMeshBuilder.Build(mask, 1f, 0.2f, false)));
        Assert.Equal(3, Quads(ItemMeshBuilder.Build(mask, 1f, 0.2f, true)));
    }

    [Fact]
    public void Rectangles_IgnoreColorsAndCoverEverySolidPixelOnce()
    {
        var mask = Mask("##..", "###.", ".###", "..##");
        var rects = ItemMeshBuilder.Rectangles(mask);
        var covered = new int[4, 4];

        foreach (var (x, y, w, h) in rects)
            for (int dy = 0; dy < h; dy++)
                for (int dx = 0; dx < w; dx++)
                    covered[x + dx, y + dy]++;

        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                Assert.Equal(mask[x, y] ? 1 : 0, covered[x, y]);
        Assert.Equal(4, rects.Count);
    }

    [Fact]
    public void SidesFollowOnlyTheSilhouette()
    {
        var mask = Mask("#", "#", "#", "#");
        var mesh = ItemMeshBuilder.Build(mask, 1f, 0.2f, false);

        Assert.Equal(6, Quads(mesh));
    }

    [Fact]
    public void FrontUvsMatchPixelPositions()
    {
        var mask = Mask("##", "##");
        var (vertices, _) = ItemMeshBuilder.Build(mask, 0.5f, 0.1f, true);

        for (int i = 0; i < 4; i++)
        {
            int o = i * ItemMeshBuilder.FloatsPerVertex;
            var position = new Vector2(vertices[o], vertices[o + 1]);
            var uv = new Vector2(vertices[o + 6], vertices[o + 7]);
            Assert.Equal(position.X, uv.X, 4);
            Assert.Equal(position.Y, uv.Y, 4);
        }
    }

    [Fact]
    public void SideUvsSampleTheEdgePixelRow()
    {
        var mask = Mask("###", "...");
        var (vertices, _) = ItemMeshBuilder.Build(mask, 1f, 0.2f, true);
        int quads = vertices.Length / ItemMeshBuilder.FloatsPerVertex / 4;

        bool found = false;
        for (int q = 0; q < quads; q++)
        {
            int o = q * 4 * ItemMeshBuilder.FloatsPerVertex;
            if (vertices[o + 4] != 1f || vertices[o + 3] != 0f) continue;
            found = true;
            for (int i = 0; i < 4; i++)
                Assert.Equal(0.25f, vertices[o + i * ItemMeshBuilder.FloatsPerVertex + 7], 4);
        }

        Assert.True(found);
    }

    [Fact]
    public void TransparentImage_HasNoGeometry()
    {
        var mesh = ItemMeshBuilder.Build(Mask("..", ".."), 1f, 0.2f, false);
        Assert.Empty(mesh.indices);
    }
}
