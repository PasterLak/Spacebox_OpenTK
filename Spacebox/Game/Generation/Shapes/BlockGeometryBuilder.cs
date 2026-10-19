using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Shapes;

public sealed class BlockGeometryBuilder
{
    private readonly List<Vector3> _positions = new();
    private readonly List<Vector3> _normals = new();
    private readonly List<Vector2> _texCoords = new();
    private readonly List<int> _indices = new();

    public BlockGeometryBuilder AddQuad(Vector3 center, Vector3 right, Vector3 up, Vector2 uvMin, Vector2 uvMax)
    {
        var normal = Vector3.Cross(right, up).Normalized();
        int start = _positions.Count;

        AddVertex(center - right - up, normal, new Vector2(uvMin.X, uvMin.Y));
        AddVertex(center + right - up, normal, new Vector2(uvMax.X, uvMin.Y));
        AddVertex(center + right + up, normal, new Vector2(uvMax.X, uvMax.Y));
        AddVertex(center - right + up, normal, new Vector2(uvMin.X, uvMax.Y));

        _indices.Add(start);
        _indices.Add(start + 1);
        _indices.Add(start + 2);
        _indices.Add(start + 2);
        _indices.Add(start + 3);
        _indices.Add(start);
        return this;
    }

    public BlockGeometryBuilder AddBox(Vector3 min, Vector3 max)
    {
        var center = (min + max) * 0.5f;
        var half = (max - min) * 0.5f;

        AddBoxFace(center + new Vector3(half.X, 0, 0), new Vector3(0, 0, -half.Z), new Vector3(0, half.Y, 0));
        AddBoxFace(center - new Vector3(half.X, 0, 0), new Vector3(0, 0, half.Z), new Vector3(0, half.Y, 0));
        AddBoxFace(center + new Vector3(0, half.Y, 0), new Vector3(half.X, 0, 0), new Vector3(0, 0, -half.Z));
        AddBoxFace(center - new Vector3(0, half.Y, 0), new Vector3(half.X, 0, 0), new Vector3(0, 0, half.Z));
        AddBoxFace(center + new Vector3(0, 0, half.Z), new Vector3(half.X, 0, 0), new Vector3(0, half.Y, 0));
        AddBoxFace(center - new Vector3(0, 0, half.Z), new Vector3(-half.X, 0, 0), new Vector3(0, half.Y, 0));
        return this;
    }

    public BlockGeometry Build()
    {
        return new BlockGeometry(_positions.ToArray(), _normals.ToArray(), _texCoords.ToArray(), _indices.ToArray());
    }

    private void AddBoxFace(Vector3 center, Vector3 right, Vector3 up)
    {
        float halfWidth = MathF.Min(0.5f, right.Length);
        float halfHeight = MathF.Min(0.5f, up.Length);
        AddQuad(center, right, up,
            new Vector2(0.5f - halfWidth, 0.5f - halfHeight),
            new Vector2(0.5f + halfWidth, 0.5f + halfHeight));
    }

    private void AddVertex(Vector3 position, Vector3 normal, Vector2 texCoord)
    {
        _positions.Add(position);
        _normals.Add(normal);
        _texCoords.Add(texCoord);
    }
}
