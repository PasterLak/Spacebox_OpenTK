using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Shapes;

public sealed class BlockGeometry
{
    public Vector3[] Positions { get; }
    public Vector3[] Normals { get; }
    public Vector2[] TexCoords { get; }
    public int[] Indices { get; }
    public Vector3 Min { get; }
    public Vector3 Max { get; }
    public Vector3 Size => Max - Min;
    public Vector3? Anchor { get; init; }
    public Vector3? FacingUp { get; init; }
    public bool RequiresSupport { get; init; }
    public Vector3 AnchorPoint => Anchor ?? (Min + Max) * 0.5f;
    public int VertexCount => Positions.Length;

    public BlockGeometry(Vector3[] positions, Vector3[] normals, Vector2[] texCoords, int[] indices)
    {
        if (positions == null || positions.Length == 0)
            throw new ArgumentException("Block geometry has no vertices", nameof(positions));
        if (normals == null || normals.Length != positions.Length)
            throw new ArgumentException("Normal count differs from vertex count", nameof(normals));
        if (texCoords == null || texCoords.Length != positions.Length)
            throw new ArgumentException("Texture coordinate count differs from vertex count", nameof(texCoords));
        if (indices == null || indices.Length == 0 || indices.Length % 3 != 0)
            throw new ArgumentException("Index count must be a positive multiple of 3", nameof(indices));

        for (int i = 0; i < indices.Length; i++)
        {
            if (indices[i] < 0 || indices[i] >= positions.Length)
                throw new ArgumentException($"Index {indices[i]} is out of range", nameof(indices));
        }

        Positions = positions;
        Normals = normals;
        TexCoords = texCoords;
        Indices = indices;

        var min = positions[0];
        var max = positions[0];
        for (int i = 1; i < positions.Length; i++)
        {
            min = Vector3.ComponentMin(min, positions[i]);
            max = Vector3.ComponentMax(max, positions[i]);
        }

        Min = min;
        Max = max;
    }
}
