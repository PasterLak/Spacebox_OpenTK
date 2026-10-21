using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class MarginContainer : Container
{
    public float MarginLeft => GetThemeConstant("margin_left", 0f);
    public float MarginTop => GetThemeConstant("margin_top", 0f);
    public float MarginRight => GetThemeConstant("margin_right", 0f);
    public float MarginBottom => GetThemeConstant("margin_bottom", 0f);

    public MarginContainer SetMargins(float all) => SetMargins(all, all, all, all);

    public MarginContainer SetMargins(float left, float top, float right, float bottom)
    {
        AddThemeConstantOverride("margin_left", left);
        AddThemeConstantOverride("margin_top", top);
        AddThemeConstantOverride("margin_right", right);
        AddThemeConstantOverride("margin_bottom", bottom);
        return this;
    }

    public override Vector2 GetMinimumSize()
    {
        return MaxChildMinimumSize() + new Vector2(MarginLeft + MarginRight, MarginTop + MarginBottom);
    }

    protected override void LayoutChildren()
    {
        var inner = Rect.Shrink(MarginLeft, MarginTop, MarginRight, MarginBottom);
        var children = Children;
        for (int i = 0; i < children.Count; i++)
            if (children[i] is Control child && child.Visible) FitChildInRect(child, inner);
    }
}

public class CenterContainer : Container
{
    public override Vector2 GetMinimumSize() => MaxChildMinimumSize();

    protected override void LayoutChildren()
    {
        var children = Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is not Control child || !child.Visible) continue;
            var min = child.GetCombinedMinimumSize();
            var position = Rect.Position + (Size - min) * 0.5f;
            child.ApplyRect(new Rect2(new Vector2(MathF.Floor(position.X), MathF.Floor(position.Y)), min));
        }
    }
}

public class PanelContainer : Container
{
    public PanelContainer()
    {
        MouseFilter = MouseFilter.Stop;
    }

    public StyleBox Style => GetThemeStyleBox("panel");

    public override Vector2 GetMinimumSize()
    {
        var style = Style;
        return MaxChildMinimumSize() + (style?.MinimumSize ?? Vector2.Zero);
    }

    protected override void LayoutChildren()
    {
        var style = Style;
        var inner = style == null ? Rect : style.ContentRect(Rect);
        var children = Children;
        for (int i = 0; i < children.Count; i++)
            if (children[i] is Control child && child.Visible) FitChildInRect(child, inner);
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        Style?.Draw(renderer, LocalRect);
    }
}
