using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public sealed class StretchSettings
{
    public Vector2 VirtualSize { get; set; } = new Vector2(1280f, 720f);
    public Vector2 MinimumViewportSize { get; set; }
    public StretchMode Mode { get; set; } = StretchMode.Expand;
    public float UiScale { get; set; } = 1f;
    public float MinScale { get; set; }
    public float MaxScale { get; set; }
    public float ScaleStep { get; set; }
    public Thickness SafeMargins { get; set; }
    public float MaxSafeAspect { get; set; }
}

public readonly struct StretchResult : IEquatable<StretchResult>
{
    public readonly Vector2 ScreenSize;
    public readonly float Scale;
    public readonly Vector2 ViewportSize;
    public readonly Transform2D ScreenTransform;
    public readonly Rect2 SafeRect;

    public StretchResult(Vector2 screenSize, float scale, Vector2 viewportSize, Transform2D screenTransform, Rect2 safeRect)
    {
        ScreenSize = screenSize;
        Scale = scale;
        ViewportSize = viewportSize;
        ScreenTransform = screenTransform;
        SafeRect = safeRect;
    }

    public bool Equals(StretchResult other)
    {
        return ScreenSize == other.ScreenSize && Scale == other.Scale && ViewportSize == other.ViewportSize
            && ScreenTransform.Origin == other.ScreenTransform.Origin && SafeRect == other.SafeRect;
    }

    public override bool Equals(object obj) => obj is StretchResult other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(ScreenSize, Scale, ViewportSize, SafeRect);
}

public static class CanvasStretch
{
    public static StretchResult Compute(StretchSettings settings, Vector2 screenSize)
    {
        var screen = Vector2.ComponentMax(screenSize, Vector2.One);
        var virtualSize = Vector2.ComponentMax(settings.VirtualSize, Vector2.One);

        float scale;
        Vector2 viewport;
        Transform2D transform;
        switch (settings.Mode)
        {
            case StretchMode.KeepAspect:
                scale = Fit(screen, virtualSize);
                viewport = virtualSize;
                var offset = (screen - virtualSize * scale) * 0.5f;
                transform = new Transform2D(new Vector2(scale, 0f), new Vector2(0f, scale), new Vector2(MathF.Floor(offset.X), MathF.Floor(offset.Y)));
                break;
            case StretchMode.Disabled:
                scale = ResolveScale(settings, screen, settings.UiScale);
                viewport = screen / scale;
                transform = Transform2D.Scaling(new Vector2(scale));
                break;
            default:
                scale = ResolveScale(settings, screen, Fit(screen, virtualSize) * settings.UiScale);
                viewport = screen / scale;
                transform = Transform2D.Scaling(new Vector2(scale));
                break;
        }

        var safe = SafeRect(settings, screen, viewport, transform);
        return new StretchResult(screen, scale, viewport, transform, safe);
    }

    public static float Fit(Vector2 screen, Vector2 size) => MathF.Min(screen.X / size.X, screen.Y / size.Y);

    private static float ResolveScale(StretchSettings settings, Vector2 screen, float scale)
    {
        if (!(scale > 0f)) scale = 1f;
        if (settings.MinScale > 0f) scale = MathF.Max(scale, settings.MinScale);
        if (settings.MaxScale > 0f) scale = MathF.Min(scale, settings.MaxScale);

        var minimum = settings.MinimumViewportSize;
        if (minimum.X > 0f && minimum.Y > 0f) scale = MathF.Min(scale, Fit(screen, minimum));

        float step = settings.ScaleStep;
        if (step > 0f && scale >= step) scale = MathF.Floor(scale / step + 1e-4f) * step;
        return scale;
    }

    private static Rect2 SafeRect(StretchSettings settings, Vector2 screen, Vector2 viewport, Transform2D transform)
    {
        var screenSafe = settings.SafeMargins.Shrink(new Rect2(Vector2.Zero, screen));
        var canvasSafe = transform.Inverse().TransformAxisAligned(screenSafe).Intersection(new Rect2(Vector2.Zero, viewport));

        float aspect = settings.MaxSafeAspect;
        if (aspect > 0f && canvasSafe.Height > 0f && canvasSafe.Width / canvasSafe.Height > aspect)
        {
            float width = canvasSafe.Height * aspect;
            canvasSafe = new Rect2(canvasSafe.X + (canvasSafe.Width - width) * 0.5f, canvasSafe.Y, width, canvasSafe.Height);
        }

        float left = MathF.Ceiling(canvasSafe.X - 1e-3f);
        float top = MathF.Ceiling(canvasSafe.Y - 1e-3f);
        float right = MathF.Floor(canvasSafe.End.X + 1e-3f);
        float bottom = MathF.Floor(canvasSafe.End.Y + 1e-3f);
        return new Rect2(left, top, MathF.Max(0f, right - left), MathF.Max(0f, bottom - top));
    }
}
