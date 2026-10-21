namespace Engine;

public sealed class ParticleDepthSorter
{
    private float[] _keys = Array.Empty<float>();
    private int[] _order = Array.Empty<int>();
    private int _count;

    public int Count => _count;

    public void Begin(int count)
    {
        if (_keys.Length < count)
        {
            int size = Math.Max(count, _keys.Length * 2);
            _keys = new float[size];
            _order = new int[size];
        }
        _count = count;
    }

    public void Set(int index, float distanceSquared)
    {
        _keys[index] = -distanceSquared;
        _order[index] = index;
    }

    public void Sort()
    {
        if (_count > 1) Array.Sort(_keys, _order, 0, _count);
    }

    public int this[int i] => _order[i];
}
