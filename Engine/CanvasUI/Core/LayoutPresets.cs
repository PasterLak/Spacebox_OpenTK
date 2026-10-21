using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public static class LayoutPresets
{
    public static (float Left, float Top, float Right, float Bottom) Anchors(LayoutPreset preset) => preset switch
    {
        LayoutPreset.TopLeft => (0f, 0f, 0f, 0f),
        LayoutPreset.TopRight => (1f, 0f, 1f, 0f),
        LayoutPreset.BottomLeft => (0f, 1f, 0f, 1f),
        LayoutPreset.BottomRight => (1f, 1f, 1f, 1f),
        LayoutPreset.CenterLeft => (0f, 0.5f, 0f, 0.5f),
        LayoutPreset.CenterTop => (0.5f, 0f, 0.5f, 0f),
        LayoutPreset.CenterRight => (1f, 0.5f, 1f, 0.5f),
        LayoutPreset.CenterBottom => (0.5f, 1f, 0.5f, 1f),
        LayoutPreset.Center => (0.5f, 0.5f, 0.5f, 0.5f),
        LayoutPreset.LeftWide => (0f, 0f, 0f, 1f),
        LayoutPreset.TopWide => (0f, 0f, 1f, 0f),
        LayoutPreset.RightWide => (1f, 0f, 1f, 1f),
        LayoutPreset.BottomWide => (0f, 1f, 1f, 1f),
        LayoutPreset.VCenterWide => (0f, 0.5f, 1f, 0.5f),
        LayoutPreset.HCenterWide => (0.5f, 0f, 0.5f, 1f),
        _ => (0f, 0f, 1f, 1f)
    };

    public static (float Left, float Top, float Right, float Bottom) Offsets(LayoutPreset preset, Vector2 size, float margin)
    {
        var (al, at, ar, ab) = Anchors(preset);
        var (left, right) = Axis(al, ar, size.X, margin);
        var (top, bottom) = Axis(at, ab, size.Y, margin);
        return (left, top, right, bottom);
    }

    public static GrowDirection GrowHorizontal(LayoutPreset preset)
    {
        var (l, _, r, _) = Anchors(preset);
        return GrowFor(l, r);
    }

    public static GrowDirection GrowVertical(LayoutPreset preset)
    {
        var (_, t, _, b) = Anchors(preset);
        return GrowFor(t, b);
    }

    private static GrowDirection GrowFor(float begin, float end)
    {
        if (begin != end) return GrowDirection.End;
        if (begin >= 1f) return GrowDirection.Begin;
        return begin > 0f ? GrowDirection.Both : GrowDirection.End;
    }

    private static (float Begin, float End) Axis(float anchorBegin, float anchorEnd, float size, float margin)
    {
        if (anchorBegin != anchorEnd) return (margin, -margin);
        if (anchorBegin <= 0f) return (margin, margin + size);
        if (anchorBegin >= 1f) return (-margin - size, -margin);
        return (-size * 0.5f, size * 0.5f);
    }
}
