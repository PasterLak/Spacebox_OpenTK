using OpenTK.Mathematics;

namespace Engine.Graphics.Icons;

public delegate bool IconSampler(Vector2 uv, Vector2 anchor, out Color4 color);

public static class IsometricRasterizer
{
    public const int FloatsPerVertex = 8;
    public const float TopLight = 0.1f;
    public const float SideShadow = 0.1f;
    private const float AlphaCutoff = 0.5f;

    public static Vector3 Project(Vector3 p) => new Vector3(p.X + p.Z, (p.Z - p.X) * 0.5f - p.Y, -p.X + p.Y + p.Z);

    public static Color4[,] Render(float[] vertices, uint[] indices, int size, IconSampler sample)
    {
        var pixels = new Color4[size, size];
        if (vertices == null || indices == null || indices.Length < 3) return pixels;

        int vertexCount = vertices.Length / FloatsPerVertex;
        var projected = new Vector3[vertexCount];
        var min = new Vector2(float.MaxValue);
        var max = new Vector2(float.MinValue);
        for (int i = 0; i < vertexCount; i++)
        {
            int o = i * FloatsPerVertex;
            projected[i] = Project(new Vector3(vertices[o], vertices[o + 1], vertices[o + 2]));
            min = Vector2.ComponentMin(min, projected[i].Xy);
            max = Vector2.ComponentMax(max, projected[i].Xy);
        }

        var extent = max - min;
        float unitScale = size / 2f;
        float scale = MathF.Min(unitScale, size / MathF.Max(extent.X, extent.Y * 1f));
        var offset = new Vector2(size, size) * 0.5f - (min + extent * 0.5f) * scale;

        var depth = new float[size, size];
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                depth[x, y] = float.MinValue;

        for (int t = 0; t + 2 < indices.Length; t += 3)
        {
            int a = (int)indices[t], b = (int)indices[t + 1], c = (int)indices[t + 2];
            var pa = projected[a].Xy * scale + offset;
            var pb = projected[b].Xy * scale + offset;
            var pc = projected[c].Xy * scale + offset;
            float area = Edge(pa, pb, pc);
            if (MathF.Abs(area) < 1e-6f) continue;

            var uvA = Uv(vertices, a);
            var uvB = Uv(vertices, b);
            var uvC = Uv(vertices, c);
            var anchor = (uvA + uvB + uvC) / 3f;
            float shade = Shade(Normal(vertices, a));

            int x0 = Math.Max(0, (int)MathF.Floor(MathF.Min(pa.X, MathF.Min(pb.X, pc.X))));
            int x1 = Math.Min(size - 1, (int)MathF.Ceiling(MathF.Max(pa.X, MathF.Max(pb.X, pc.X))));
            int y0 = Math.Max(0, (int)MathF.Floor(MathF.Min(pa.Y, MathF.Min(pb.Y, pc.Y))));
            int y1 = Math.Min(size - 1, (int)MathF.Ceiling(MathF.Max(pa.Y, MathF.Max(pb.Y, pc.Y))));

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float wa = Edge(pb, pc, p) / area;
                    float wb = Edge(pc, pa, p) / area;
                    float wc = 1f - wa - wb;
                    if (wa < -1e-4f || wb < -1e-4f || wc < -1e-4f) continue;

                    float z = projected[a].Z * wa + projected[b].Z * wb + projected[c].Z * wc;
                    if (z <= depth[x, y]) continue;

                    var uv = uvA * wa + uvB * wb + uvC * wc;
                    if (!sample(uv, anchor, out var color) || color.A < AlphaCutoff) continue;

                    depth[x, y] = z;
                    pixels[x, y] = Lit(color, shade);
                }
            }
        }

        return pixels;
    }

    public static float Shade(Vector3 normal)
    {
        return TopLight * MathF.Max(0f, normal.Y) - SideShadow * MathF.Max(0f, -normal.X) - SideShadow * MathF.Max(0f, -normal.Y);
    }

    private static Color4 Lit(Color4 color, float shade)
    {
        return new Color4(Math.Clamp(color.R + shade, 0f, 1f), Math.Clamp(color.G + shade, 0f, 1f), Math.Clamp(color.B + shade, 0f, 1f), 1f);
    }

    private static float Edge(Vector2 a, Vector2 b, Vector2 p) => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);

    private static Vector2 Uv(float[] v, int i) => new Vector2(v[i * FloatsPerVertex + 6], v[i * FloatsPerVertex + 7]);

    private static Vector3 Normal(float[] v, int i) => new Vector3(v[i * FloatsPerVertex + 3], v[i * FloatsPerVertex + 4], v[i * FloatsPerVertex + 5]);
}
