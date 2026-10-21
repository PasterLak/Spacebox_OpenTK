using OpenTK.Mathematics;

namespace Engine.Graphics.Transparency;

public static class TransparentQueue
{
    private static TransparentEntry[] _entries = new TransparentEntry[256];
    private static readonly Stack<(int Start, Vector3 ViewPosition)> _scopes = new();
    private static int _count;
    private static int _sequence;
    private static Vector3 _viewPosition;
    private static bool _flushing;

    public static bool IsCollecting => _scopes.Count > 0 && !_flushing;
    public static int Count => _count;

    public static void BeginScope(Vector3 viewPosition)
    {
        _scopes.Push((_count, viewPosition));
        _viewPosition = viewPosition;
    }

    public static void BeginScope()
    {
        BeginScope(CurrentViewPosition());
    }

    public static bool TrySubmit(ITransparentRenderable renderable, Vector3 renderPosition, int queue = 0)
    {
        if (!IsCollecting || renderable == null) return false;

        if (_count == _entries.Length)
            Array.Resize(ref _entries, _entries.Length * 2);

        _entries[_count++] = new TransparentEntry
        {
            Renderable = renderable,
            Queue = queue,
            DistanceSquared = Vector3.DistanceSquared(renderPosition, _viewPosition),
            Sequence = _sequence++
        };
        return true;
    }

    public static void EndScope()
    {
        if (_scopes.Count == 0) return;

        int start = _scopes.Pop().Start;
        int length = _count - start;
        if (length > 1)
            Array.Sort(_entries, start, length, TransparentEntryComparer.Instance);

        _flushing = true;
        try
        {
            for (int i = start; i < _count; i++)
                _entries[i].Renderable.RenderTransparent();
        }
        finally
        {
            _flushing = false;
            Array.Clear(_entries, start, length);
            _count = start;
            if (_scopes.Count == 0) _sequence = 0;
            else _viewPosition = _scopes.Peek().ViewPosition;
        }
    }

    public static void Clear()
    {
        Array.Clear(_entries, 0, _count);
        _count = 0;
        _sequence = 0;
        _flushing = false;
        _scopes.Clear();
    }

    private static Vector3 CurrentViewPosition()
    {
        var cam = Camera.Main;
        return cam == null ? Vector3.Zero : RenderSpace.ToRender(cam.PositionWorld);
    }
}
