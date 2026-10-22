using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class HFlowContainer : Container
{
    private float _measuredHeight;

    public float HSeparation => GetThemeConstant("h_separation", 4f);
    public float VSeparation => GetThemeConstant("v_separation", 4f);

    public override Vector2 GetMinimumSize()
    {
        var widest = MaxChildMinimumSize();
        float height = Arrange(Size.X > 0f ? Size.X : float.MaxValue, false);
        _measuredHeight = height;
        return new Vector2(widest.X, height);
    }

    protected override void LayoutChildren()
    {
        float height = Arrange(Size.X, true);
        if (MathF.Abs(height - _measuredHeight) > 0.5f)
        {
            _measuredHeight = height;
            QueueLayout();
        }
    }

    private float Arrange(float width, bool place)
    {
        float x = 0f;
        float y = 0f;
        float rowHeight = 0f;
        var children = Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is not Control child || !child.Visible) continue;
            var min = child.GetCombinedMinimumSize();
            if (x > 0f && x + min.X > width)
            {
                x = 0f;
                y += rowHeight + VSeparation;
                rowHeight = 0f;
            }

            if (place) FitChildInRect(child, new Rect2(Rect.X + x, Rect.Y + y, min.X, min.Y));
            x += min.X + HSeparation;
            rowHeight = MathF.Max(rowHeight, min.Y);
        }
        return y + rowHeight;
    }
}
