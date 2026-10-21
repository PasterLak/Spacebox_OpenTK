using OpenTK.Mathematics;

namespace Spacebox.Game;

public sealed class ItemMeshBuilder
{
    public const int FloatsPerVertex = 8;

    private static readonly Vector3 FaceNormal = Vector3.One;

    private readonly bool[,] _solid;
    private readonly int _width;
    private readonly int _height;
    private readonly float _scale;
    private readonly float _depth;
    private readonly List<float> _vertices = new List<float>();
    private readonly List<uint> _indices = new List<uint>();

    private ItemMeshBuilder(bool[,] solid, float scale, float depth)
    {
        _solid = solid;
        _width = solid.GetLength(0);
        _height = solid.GetLength(1);
        _scale = scale;
        _depth = depth;
    }

    public static bool[,] SolidMask(Color4[,] pixels)
    {
        int width = pixels.GetLength(0);
        int height = pixels.GetLength(1);
        var solid = new bool[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                solid[x, y] = pixels[x, y].A >= 1f;
        return solid;
    }

    public static (float[] vertices, uint[] indices) Build(bool[,] solid, float scale, float depth, bool onlyVisibleSides)
    {
        var builder = new ItemMeshBuilder(solid, scale, depth);
        builder.AddFlatFaces(onlyVisibleSides);
        builder.AddVerticalSides(-1);
        builder.AddHorizontalSides(1);
        if (!onlyVisibleSides)
        {
            builder.AddVerticalSides(1);
            builder.AddHorizontalSides(-1);
        }
        return (builder._vertices.ToArray(), builder._indices.ToArray());
    }

    public static List<(int x, int y, int width, int height)> Rectangles(bool[,] solid)
    {
        int width = solid.GetLength(0);
        int height = solid.GetLength(1);
        var used = new bool[width, height];
        var result = new List<(int, int, int, int)>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (used[x, y] || !solid[x, y]) continue;

                int w = 1;
                while (x + w < width && solid[x + w, y] && !used[x + w, y]) w++;

                int h = 1;
                while (y + h < height && RowFree(solid, used, x, y + h, w)) h++;

                for (int dy = 0; dy < h; dy++)
                    for (int dx = 0; dx < w; dx++)
                        used[x + dx, y + dy] = true;

                result.Add((x, y, w, h));
            }
        }

        return result;
    }

    private static bool RowFree(bool[,] solid, bool[,] used, int x, int y, int w)
    {
        for (int i = 0; i < w; i++)
            if (!solid[x + i, y] || used[x + i, y]) return false;
        return true;
    }

    private bool IsSolid(int x, int y) => x >= 0 && y >= 0 && x < _width && y < _height && _solid[x, y];

    private void AddFlatFaces(bool onlyVisibleSides)
    {
        foreach (var (x, y, w, h) in Rectangles(_solid))
        {
            var bl = Point(x, y, 0f);
            var br = Point(x + w, y, 0f);
            var tr = Point(x + w, y + h, 0f);
            var tl = Point(x, y + h, 0f);
            AddQuad(tl, tr, br, bl, FaceNormal, Uv(x, y + h), Uv(x + w, y + h), Uv(x + w, y), Uv(x, y));

            if (onlyVisibleSides) continue;

            bl.Z = br.Z = tr.Z = tl.Z = _depth;
            AddQuad(tl, bl, br, tr, FaceNormal, Uv(x, y + h), Uv(x, y), Uv(x + w, y), Uv(x + w, y + h));
        }
    }

    private void AddVerticalSides(int side)
    {
        for (int x = 0; x < _width; x++)
        {
            int start = -1;
            for (int y = 0; y <= _height; y++)
            {
                bool edge = y < _height && IsSolid(x, y) && !IsSolid(x + side, y);
                if (edge && start < 0) start = y;
                if (edge || start < 0) continue;

                AddVerticalSide(x, start, y, side);
                start = -1;
            }
        }
    }

    private void AddHorizontalSides(int side)
    {
        for (int y = 0; y < _height; y++)
        {
            int start = -1;
            for (int x = 0; x <= _width; x++)
            {
                bool edge = x < _width && IsSolid(x, y) && !IsSolid(x, y + side);
                if (edge && start < 0) start = x;
                if (edge || start < 0) continue;

                AddHorizontalSide(y, start, x, side);
                start = -1;
            }
        }
    }

    private void AddVerticalSide(int x, int y0, int y1, int side)
    {
        float px = side < 0 ? x : x + 1;
        float u = x + 0.5f;
        var low = Uv(u, y0);
        var high = Uv(u, y1);

        if (side < 0)
            AddQuad(Point(px, y0, 0f), Point(px, y0, _depth), Point(px, y1, _depth), Point(px, y1, 0f), -Vector3.UnitX, low, low, high, high);
        else
            AddQuad(Point(px, y0, 0f), Point(px, y1, 0f), Point(px, y1, _depth), Point(px, y0, _depth), Vector3.UnitX, low, high, high, low);
    }

    private void AddHorizontalSide(int y, int x0, int x1, int side)
    {
        float py = side > 0 ? y + 1 : y;
        float v = y + 0.5f;
        var left = Uv(x0, v);
        var right = Uv(x1, v);

        if (side > 0)
            AddQuad(Point(x0, py, 0f), Point(x0, py, _depth), Point(x1, py, _depth), Point(x1, py, 0f), Vector3.UnitY, left, left, right, right);
        else
            AddQuad(Point(x0, py, 0f), Point(x1, py, 0f), Point(x1, py, _depth), Point(x0, py, _depth), -Vector3.UnitY, left, right, right, left);
    }

    private Vector3 Point(float x, float y, float z) => new Vector3(x * _scale, y * _scale, z);

    private Vector2 Uv(float x, float y) => new Vector2(x / _width, y / _height);

    private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
    {
        uint start = (uint)(_vertices.Count / FloatsPerVertex);
        AddVertex(a, normal, uvA);
        AddVertex(b, normal, uvB);
        AddVertex(c, normal, uvC);
        AddVertex(d, normal, uvD);

        _indices.Add(start);
        _indices.Add(start + 1);
        _indices.Add(start + 2);
        _indices.Add(start);
        _indices.Add(start + 2);
        _indices.Add(start + 3);
    }

    private void AddVertex(Vector3 position, Vector3 normal, Vector2 uv)
    {
        _vertices.Add(position.X);
        _vertices.Add(position.Y);
        _vertices.Add(position.Z);
        _vertices.Add(normal.X);
        _vertices.Add(normal.Y);
        _vertices.Add(normal.Z);
        _vertices.Add(uv.X);
        _vertices.Add(uv.Y);
    }
}
