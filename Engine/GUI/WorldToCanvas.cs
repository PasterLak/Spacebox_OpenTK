using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.GUI;

public readonly struct WorldToCanvas
{
    private readonly Camera _camera;
    private readonly CanvasRoot _root;
    private readonly Vector2 _origin;

    public WorldToCanvas(Camera camera, CanvasRoot root, Vector2 origin)
    {
        _camera = camera;
        _root = root;
        _origin = origin;
    }

    public Camera Camera => _camera;

    public bool TryProject(Vector3 world, out Vector2 canvas)
    {
        var screen = _camera.WorldToScreenPoint(world);
        if (screen == Vector2.Zero)
        {
            canvas = default;
            return false;
        }

        canvas = _root.ScreenToCanvas(screen) - _origin;
        return true;
    }
}
