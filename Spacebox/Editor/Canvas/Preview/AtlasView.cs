using Engine;
using Engine.CanvasUI;
using Engine.Graphics.Atlas;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Editor.Canvas.Preview;

public sealed class AtlasView : Control
{
    public const float MinZoom = 0.05f;
    public const float MaxZoom = 8f;

    private static readonly Color4 HoverColor = new Color4(1f, 1f, 0f, 1f);
    private static readonly Color4 SelectedColor = new Color4(1f, 0.65f, 0f, 1f);

    private Vector2 _offset;
    private Vector2 _lastPointer;
    private AtlasRegion _hovered;
    private bool _dragged;

    public AtlasView()
    {
        ClipContents = true;
        MouseFilter = MouseFilter.Stop;
        CustomMinimumSize = new Vector2(160f, 160f);
    }

    public TextureAtlas Atlas { get; set; }
    public Texture2D Texture { get; set; }
    public float Zoom { get; set; } = 1f;
    public bool Fit { get; set; } = true;
    public string SelectedRegion { get; set; }

    public event Action<string> RegionClicked;
    public event Action ZoomChangedByUser;

    public static float FitZoom(Texture2D texture, Vector2 available)
    {
        if (texture == null || texture.Width <= 0 || texture.Height <= 0 || available.X <= 1f || available.Y <= 1f) return 1f;
        return Math.Clamp(MathF.Min(available.X / texture.Width, available.Y / texture.Height), MinZoom, MaxZoom);
    }

    public override void Process(float deltaTime)
    {
        if (!Fit || Texture == null) return;
        Zoom = FitZoom(Texture, Size);
        _offset = Vector2.Zero;
    }

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left) return false;
        _lastPointer = localPosition;
        _dragged = false;
        return true;
    }

    protected override void OnMouseMove(Vector2 localPosition, bool pressed)
    {
        if (pressed && IsMouseDown && !Fit)
        {
            var delta = localPosition - _lastPointer;
            if (delta.LengthSquared > 0f) _dragged = true;
            _offset += delta;
        }
        _lastPointer = localPosition;

        _hovered = FindRegion(localPosition);
        TooltipText = _hovered == null ? "" : $"{_hovered.Name}  {_hovered.Width}x{_hovered.Height}";
    }

    protected override void OnMouseExit()
    {
        _hovered = null;
    }

    protected override bool OnMouseUp(Vector2 localPosition, MouseButton button, bool inside)
    {
        if (button == MouseButton.Left && inside && !_dragged && _hovered != null)
        {
            SelectedRegion = _hovered.Name;
            RegionClicked?.Invoke(_hovered.Name);
        }
        return true;
    }

    protected override bool OnScroll(Vector2 localPosition, float delta)
    {
        float previous = Zoom;
        Zoom = Math.Clamp(Zoom * MathF.Pow(1.15f, delta), MinZoom, MaxZoom);
        _offset = localPosition - (localPosition - _offset) * (Zoom / previous);
        Fit = false;
        ZoomChangedByUser?.Invoke();
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        renderer.DrawRect(LocalRect, new Color4(0.04f, 0.045f, 0.055f, 1f));
        if (Texture == null) return;

        var image = ImageRect();
        renderer.DrawRect(image, new Color4(0.125f, 0.125f, 0.125f, 1f));
        renderer.DrawTexture(Texture, image, new Rect2(0f, 1f, 1f, -1f), Color4.White);
        if (_hovered != null) Outline(renderer, _hovered, HoverColor);
        if (SelectedRegion != null && Atlas != null && Atlas.TryGetRegion(SelectedRegion, out var selected)) Outline(renderer, selected, SelectedColor);
    }

    private Rect2 ImageRect() => new Rect2(_offset.X, _offset.Y, Texture.Width * Zoom, Texture.Height * Zoom);

    private void Outline(ICanvasRenderer renderer, AtlasRegion region, Color4 color)
    {
        var image = ImageRect();
        float x0 = image.X + region.UvMin.X * image.Width;
        float x1 = image.X + region.UvMax.X * image.Width;
        float y0 = image.Y + (1f - region.UvMax.Y) * image.Height;
        float y1 = image.Y + (1f - region.UvMin.Y) * image.Height;
        const float t = 2f;
        renderer.DrawRect(new Rect2(x0, y0, x1 - x0, t), color);
        renderer.DrawRect(new Rect2(x0, y1 - t, x1 - x0, t), color);
        renderer.DrawRect(new Rect2(x0, y0, t, y1 - y0), color);
        renderer.DrawRect(new Rect2(x1 - t, y0, t, y1 - y0), color);
    }

    private AtlasRegion FindRegion(Vector2 localPosition)
    {
        if (Atlas == null || Texture == null) return null;
        var image = ImageRect();
        if (!image.Contains(localPosition)) return null;

        float u = (localPosition.X - image.X) / image.Width;
        float v = 1f - (localPosition.Y - image.Y) / image.Height;
        foreach (var name in Atlas.Names)
        {
            if (!Atlas.TryGetRegion(name, out var region)) continue;
            if (u >= region.UvMin.X && u <= region.UvMax.X && v >= region.UvMin.Y && v <= region.UvMax.Y) return region;
        }
        return null;
    }
}
