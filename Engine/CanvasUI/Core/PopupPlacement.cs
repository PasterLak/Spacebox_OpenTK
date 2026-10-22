using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public enum PopupSide
{
    Below,
    Above,
    Right,
    Left
}

public static class PopupPlacement
{
    public static Rect2 Place(Rect2 anchor, Vector2 size, Rect2 bounds, PopupSide preferred, float gap = 0f)
    {
        bool vertical = preferred == PopupSide.Below || preferred == PopupSide.Above;
        var side = ChooseSide(anchor, size, bounds, preferred, gap, vertical);

        float x, y;
        if (vertical)
        {
            y = side == PopupSide.Below ? anchor.End.Y + gap : anchor.Y - gap - size.Y;
            x = Clamp(anchor.X, bounds.X, bounds.End.X - size.X);
            y = Clamp(y, bounds.Y, bounds.End.Y - size.Y);
        }
        else
        {
            x = side == PopupSide.Right ? anchor.End.X + gap : anchor.X - gap - size.X;
            y = Clamp(anchor.Y, bounds.Y, bounds.End.Y - size.Y);
            x = Clamp(x, bounds.X, bounds.End.X - size.X);
        }

        return new Rect2(MathF.Floor(x), MathF.Floor(y), size.X, size.Y);
    }

    private static PopupSide ChooseSide(Rect2 anchor, Vector2 size, Rect2 bounds, PopupSide preferred, float gap, bool vertical)
    {
        float before = vertical ? anchor.Y - gap - bounds.Y : anchor.X - gap - bounds.X;
        float after = vertical ? bounds.End.Y - anchor.End.Y - gap : bounds.End.X - anchor.End.X - gap;
        float needed = vertical ? size.Y : size.X;
        bool preferAfter = preferred == PopupSide.Below || preferred == PopupSide.Right;
        var afterSide = vertical ? PopupSide.Below : PopupSide.Right;
        var beforeSide = vertical ? PopupSide.Above : PopupSide.Left;

        if (preferAfter && after >= needed) return afterSide;
        if (!preferAfter && before >= needed) return beforeSide;
        if (preferAfter && before >= needed) return beforeSide;
        if (!preferAfter && after >= needed) return afterSide;
        return after >= before ? afterSide : beforeSide;
    }

    private static float Clamp(float value, float min, float max) => max < min ? min : Math.Clamp(value, min, max);
}
