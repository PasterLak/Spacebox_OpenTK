using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public abstract class StyleBox
{
    public float ContentMarginLeft { get; set; }
    public float ContentMarginTop { get; set; }
    public float ContentMarginRight { get; set; }
    public float ContentMarginBottom { get; set; }

    public Vector2 MinimumSize => new Vector2(ContentMarginLeft + ContentMarginRight, ContentMarginTop + ContentMarginBottom);

    public StyleBox SetContentMargins(float all) => SetContentMargins(all, all, all, all);

    public StyleBox SetContentMargins(float horizontal, float vertical) => SetContentMargins(horizontal, vertical, horizontal, vertical);

    public StyleBox SetContentMargins(float left, float top, float right, float bottom)
    {
        ContentMarginLeft = left;
        ContentMarginTop = top;
        ContentMarginRight = right;
        ContentMarginBottom = bottom;
        return this;
    }

    public Rect2 ContentRect(Rect2 rect) => rect.Shrink(ContentMarginLeft, ContentMarginTop, ContentMarginRight, ContentMarginBottom);

    public abstract void Draw(ICanvasRenderer renderer, Rect2 rect);
}

public sealed class StyleBoxEmpty : StyleBox
{
    public override void Draw(ICanvasRenderer renderer, Rect2 rect)
    {
    }
}
