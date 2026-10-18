using OpenTK.Mathematics;

namespace Engine.AI;

public sealed class VoxelPath
{
    private readonly List<Vector3> _points = new List<Vector3>(16);

    public int Count => _points.Count;
    public bool IsEmpty => _points.Count == 0;
    public Vector3 this[int index] => _points[index];
    public Vector3 Destination => _points[_points.Count - 1];

    public void Clear()
    {
        _points.Clear();
    }

    public void Add(Vector3 point)
    {
        _points.Add(point);
    }
}
