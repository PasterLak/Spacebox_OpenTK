using Engine;

namespace Spacebox.Game.Rendering;

public static class FlashlightBeamMesh
{
    public const int Segments = 32;
    public const int Rings = 8;

    public static float[] BuildVertices()
    {
        var vertices = new float[(Segments + 1) * (Rings + 1) * 8];
        int write = 0;
        for (int ring = 0; ring <= Rings; ring++)
        {
            float z = ring / (float)Rings;
            for (int segment = 0; segment <= Segments; segment++)
            {
                float u = segment / (float)Segments;
                float angle = u * MathF.Tau;
                float x = MathF.Cos(angle);
                float y = MathF.Sin(angle);

                vertices[write++] = x * z;
                vertices[write++] = y * z;
                vertices[write++] = z;
                vertices[write++] = x;
                vertices[write++] = y;
                vertices[write++] = 0f;
                vertices[write++] = u;
                vertices[write++] = z;
            }
        }
        return vertices;
    }

    public static int[] BuildIndices()
    {
        var indices = new int[Segments * Rings * 6];
        int write = 0;
        int stride = Segments + 1;
        for (int ring = 0; ring < Rings; ring++)
        {
            for (int segment = 0; segment < Segments; segment++)
            {
                int a = ring * stride + segment;
                int b = a + 1;
                int c = a + stride;
                int d = c + 1;
                indices[write++] = a;
                indices[write++] = c;
                indices[write++] = b;
                indices[write++] = b;
                indices[write++] = c;
                indices[write++] = d;
            }
        }
        return indices;
    }

    public static Mesh Create()
    {
        var mesh = new Mesh(BuildVertices(), BuildIndices());
        Resources.AddResourceToDispose(mesh);
        return mesh;
    }
}
