namespace Engine.CanvasUI;

internal static class ThemeVersion
{
    private static int _current;

    public static int Current => Volatile.Read(ref _current);

    public static void Invalidate() => Interlocked.Increment(ref _current);
}

internal sealed class ThemeLookupCache<T>
{
    private string[] _names = new string[2];
    private T[] _values = new T[2];
    private int _count;
    private int _version = -1;

    public bool TryGet(int version, string name, out T value)
    {
        if (_version != version)
        {
            Clear(version);
            value = default;
            return false;
        }

        for (int i = 0; i < _count; i++)
        {
            if (!ReferenceEquals(_names[i], name) && !string.Equals(_names[i], name)) continue;
            value = _values[i];
            return true;
        }
        value = default;
        return false;
    }

    public void Store(int version, string name, T value)
    {
        if (_version != version) Clear(version);
        if (_count == _names.Length)
        {
            Array.Resize(ref _names, _count * 2);
            Array.Resize(ref _values, _count * 2);
        }
        _names[_count] = name;
        _values[_count] = value;
        _count++;
    }

    private void Clear(int version)
    {
        Array.Clear(_names, 0, _count);
        Array.Clear(_values, 0, _count);
        _count = 0;
        _version = version;
    }
}
