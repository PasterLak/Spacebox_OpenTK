using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public sealed class ShipSnapshotBuffer
{
    private const int Capacity = 16;
    private const float MaxExtrapolation = 0.25f;

    private readonly double[] _times = new double[Capacity];
    private readonly Vector3[] _positions = new Vector3[Capacity];
    private readonly Quaternion[] _orientations = new Quaternion[Capacity];
    private readonly Vector3[] _velocities = new Vector3[Capacity];
    private int _count;
    private int _head;

    public int Count => _count;

    public void Add(double time, Vector3 position, Quaternion orientation, Vector3 velocity)
    {
        if (_count > 0 && time <= _times[Index(_count - 1)]) return;

        int index = (_head + _count) % Capacity;
        if (_count == Capacity)
        {
            _head = (_head + 1) % Capacity;
            index = (_head + _count - 1) % Capacity;
        }
        else
        {
            _count++;
        }

        _times[index] = time;
        _positions[index] = position;
        _orientations[index] = orientation;
        _velocities[index] = velocity;
    }

    public bool TrySample(double time, out Vector3 position, out Quaternion orientation)
    {
        position = Vector3.Zero;
        orientation = Quaternion.Identity;
        if (_count == 0) return false;

        int first = Index(0);
        if (time <= _times[first])
        {
            position = _positions[first];
            orientation = _orientations[first];
            return true;
        }

        for (int i = 1; i < _count; i++)
        {
            int a = Index(i - 1);
            int b = Index(i);
            if (time > _times[b]) continue;

            float t = (float)((time - _times[a]) / (_times[b] - _times[a]));
            position = Vector3.Lerp(_positions[a], _positions[b], t);
            orientation = Quaternion.Slerp(_orientations[a], _orientations[b], t);
            return true;
        }

        int last = Index(_count - 1);
        float ahead = (float)Math.Min(time - _times[last], MaxExtrapolation);
        position = _positions[last] + _velocities[last] * ahead;
        orientation = _orientations[last];
        return true;
    }

    public void Clear()
    {
        _count = 0;
        _head = 0;
    }

    private int Index(int offset) => (_head + offset) % Capacity;
}
