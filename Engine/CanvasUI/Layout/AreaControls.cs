namespace Engine.CanvasUI;

public abstract class AreaControl : Control
{
    public Rect2 ContentRect { get; private set; }

    protected AreaControl()
    {
        MouseFilter = MouseFilter.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    protected abstract Rect2 ComputeContentRect(Rect2 rect);

    protected override void LayoutChildren()
    {
        ContentRect = ComputeContentRect(Rect);
        var children = Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is Control child && child.Visible) child.ApplyRect(child.ComputeAnchoredRect(ContentRect));
            else if (children[i] is not Control) LayoutNested(children[i]);
        }
    }
}

public class SafeArea : AreaControl
{
    private Thickness _padding;

    public Thickness Padding
    {
        get => _padding;
        set
        {
            if (_padding == value) return;
            _padding = value;
            QueueLayout();
        }
    }

    protected override Rect2 ComputeContentRect(Rect2 rect)
    {
        var area = Root == null ? rect : rect.Intersection(Root.SafeRect);
        return _padding.Shrink(area);
    }
}

public class ExclusionArea : SafeArea
{
    private readonly List<Control> _exclusions = new List<Control>();
    private readonly List<Control> _exclusionGroups = new List<Control>();
    private float _gap;

    public float Gap
    {
        get => _gap;
        set
        {
            if (_gap == value) return;
            _gap = value;
            QueueLayout();
        }
    }

    public ExclusionArea Exclude(Control control)
    {
        if (control != null && !_exclusions.Contains(control)) _exclusions.Add(control);
        QueueLayout();
        return this;
    }

    public ExclusionArea ExcludeChildrenOf(Control group)
    {
        if (group != null && !_exclusionGroups.Contains(group)) _exclusionGroups.Add(group);
        QueueLayout();
        return this;
    }

    public void ClearExclusions()
    {
        _exclusions.Clear();
        _exclusionGroups.Clear();
        QueueLayout();
    }

    protected override Rect2 ComputeContentRect(Rect2 rect)
    {
        var area = base.ComputeContentRect(rect);
        for (int i = 0; i < _exclusions.Count; i++) area = Avoid(area, _exclusions[i]);
        for (int g = 0; g < _exclusionGroups.Count; g++)
        {
            var children = _exclusionGroups[g].Children;
            for (int i = 0; i < children.Count; i++)
                if (children[i] is Control child) area = Avoid(area, child);
        }
        return area;
    }

    private Rect2 Avoid(Rect2 area, Control obstacle)
    {
        if (obstacle == this || !obstacle.IsVisibleInTree || !obstacle.Rect.HasArea) return area;
        return RectCut.LargestOutside(area, obstacle.Rect.Grow(_gap, _gap, _gap, _gap));
    }
}

public static class RectCut
{
    public static Rect2 LargestOutside(Rect2 area, Rect2 obstacle)
    {
        if (!area.Intersects(obstacle)) return area;

        var best = new Rect2(area.X, area.Y, area.Width, MathF.Max(0f, obstacle.Y - area.Y));
        Consider(ref best, new Rect2(area.X, obstacle.End.Y, area.Width, MathF.Max(0f, area.End.Y - obstacle.End.Y)));
        Consider(ref best, new Rect2(area.X, area.Y, MathF.Max(0f, obstacle.X - area.X), area.Height));
        Consider(ref best, new Rect2(obstacle.End.X, area.Y, MathF.Max(0f, area.End.X - obstacle.End.X), area.Height));
        return best;
    }

    private static void Consider(ref Rect2 best, Rect2 candidate)
    {
        if (candidate.Area > best.Area) best = candidate;
    }
}
