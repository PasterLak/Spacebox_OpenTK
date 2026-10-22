using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public abstract class Container : Control
{
    protected Container()
    {
        MouseFilter = MouseFilter.Pass;
    }

    public static void FitChildInRect(Control child, Rect2 rect)
    {
        var min = child.GetCombinedMinimumSize();
        float x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height;

        if ((child.SizeFlagsHorizontal & SizeFlags.Fill) == 0)
        {
            w = MathF.Min(min.X, rect.Width);
            if ((child.SizeFlagsHorizontal & SizeFlags.ShrinkCenter) != 0) x += (rect.Width - w) * 0.5f;
            else if ((child.SizeFlagsHorizontal & SizeFlags.ShrinkEnd) != 0) x += rect.Width - w;
        }

        if ((child.SizeFlagsVertical & SizeFlags.Fill) == 0)
        {
            h = MathF.Min(min.Y, rect.Height);
            if ((child.SizeFlagsVertical & SizeFlags.ShrinkCenter) != 0) y += (rect.Height - h) * 0.5f;
            else if ((child.SizeFlagsVertical & SizeFlags.ShrinkEnd) != 0) y += rect.Height - h;
        }

        var max = child.CustomMaximumSize;
        if (max.X > 0f) LimitAxis(ref x, ref w, MathF.Max(max.X, min.X), child.SizeFlagsHorizontal);
        if (max.Y > 0f) LimitAxis(ref y, ref h, MathF.Max(max.Y, min.Y), child.SizeFlagsVertical);

        child.ApplyRect(new Rect2(MathF.Round(x), MathF.Round(y), MathF.Round(w), MathF.Round(h)));
    }

    private static void LimitAxis(ref float position, ref float size, float max, SizeFlags flags)
    {
        if (size <= max) return;
        float free = size - max;
        size = max;
        if ((flags & SizeFlags.ShrinkCenter) != 0) position += free * 0.5f;
        else if ((flags & SizeFlags.ShrinkEnd) != 0) position += free;
    }

    protected int CountLayoutChildren()
    {
        int count = 0;
        var children = Children;
        for (int i = 0; i < children.Count; i++)
            if (children[i] is Control control && control.Visible) count++;
        return count;
    }

    protected Vector2 MaxChildMinimumSize()
    {
        var max = Vector2.Zero;
        var children = Children;
        for (int i = 0; i < children.Count; i++)
            if (children[i] is Control control && control.Visible) max = Vector2.ComponentMax(max, control.GetCombinedMinimumSize());
        return max;
    }
}
