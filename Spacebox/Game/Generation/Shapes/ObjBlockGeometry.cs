using Engine.Utils;
using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Shapes;

public static class ObjBlockGeometry
{
    private const int Stride = 8;

    public static BlockGeometry Load(string path)
    {
        var (vertices, indices) = ObjLoader.Load(path);
        return FromInterleaved(vertices, indices);
    }

    public static BlockGeometry FromInterleaved(float[] vertices, int[] indices)
    {
        if (vertices == null || vertices.Length % Stride != 0)
            throw new ArgumentException("Vertex data is not a multiple of the OBJ vertex stride", nameof(vertices));

        int count = vertices.Length / Stride;
        var positions = new Vector3[count];
        var normals = new Vector3[count];
        var texCoords = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            int o = i * Stride;
            positions[i] = new Vector3(vertices[o], vertices[o + 1], vertices[o + 2]);
            var normal = new Vector3(vertices[o + 3], vertices[o + 4], vertices[o + 5]);
            normals[i] = normal.LengthSquared > 1e-8f ? normal.Normalized() : Vector3.UnitY;
            texCoords[i] = new Vector2(vertices[o + 6], 1f - vertices[o + 7]);
        }

        return new BlockGeometry(positions, normals, texCoords, indices);
    }
}
