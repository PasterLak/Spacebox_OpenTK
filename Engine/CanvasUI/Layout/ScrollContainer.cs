using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class ScrollContainer : Container
{
    private float _scroll;

    public float ScrollStep { get; set; } = 40f;
    public float ScrollbarWidth => GetThemeConstant("scrollbar_width", 6f);
    public float ContentHeight { get; private set; }
    public float MaxScroll => MathF.Max(0f, ContentHeight - Size.Y);
    public bool IsOverflowing => MaxScroll > 0.5f;

    public ScrollContainer()
    {
        ClipContents = true;
        MouseFilter = MouseFilter.Pass;
    }

    public float ScrollOffset
    {
        get => _scroll;
        set
        {
            float clamped = Math.Clamp(value, 0f, MaxScroll);
            if (_scroll == clamped) return;
            _scroll = clamped;
            QueueLayout();
        }
    }

    public override Vector2 GetMinimumSize()
    {
        var min = MaxChildMinimumSize();
        return new Vector2(min.X + ScrollbarWidth, 0f);
    }

    public void EnsureVisible(Control descendant)
    {
        if (descendant == null) return;
        float top = descendant.Rect.Y - Rect.Y;
        float bottom = descendant.Rect.End.Y - Rect.Y;
        if (top < 0f) ScrollOffset = _scroll + top;
        else if (bottom > Size.Y) ScrollOffset = _scroll + bottom - Size.Y;
    }

    protected override void LayoutChildren()
    {
        var min = MaxChildMinimumSize();
        ContentHeight = MathF.Max(min.Y, Size.Y);
        _scroll = Math.Clamp(_scroll, 0f, MaxScroll);
        float width = Size.X - (IsOverflowing ? ScrollbarWidth : 0f);

        var children = Children;
        for (int i = 0; i < children.Count; i++)
            if (children[i] is Control child && child.Visible) FitChildInRect(child, new Rect2(Rect.X, Rect.Y - _scroll, width, ContentHeight));
    }

    protected internal override bool OnScroll(Vector2 localPosition, float delta)
    {
        if (!IsOverflowing) return false;
        ScrollOffset = _scroll - delta * ScrollStep;
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        if (!IsOverflowing) return;
        float width = ScrollbarWidth;
        var track = new Rect2(Size.X - width, 0f, width, Size.Y);
        renderer.DrawRect(track, GetThemeColor("scroll_track", new Color4(1f, 1f, 1f, 0.06f)));
        float thumbHeight = MathF.Max(12f, Size.Y * Size.Y / ContentHeight);
        float thumbY = MaxScroll > 0f ? (Size.Y - thumbHeight) * _scroll / MaxScroll : 0f;
        renderer.DrawRect(new Rect2(track.X, MathF.Floor(thumbY), width, MathF.Floor(thumbHeight)), GetThemeColor("scroll_thumb", new Color4(1f, 1f, 1f, 0.35f)));
    }
}
