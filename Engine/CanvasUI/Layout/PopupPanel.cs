using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class PopupPanel : PanelContainer
{
    private Rect2 _anchorRect;
    private PopupSide _side = PopupSide.Below;
    private float _gap = 4f;

    public Rect2 AnchorRect { get => _anchorRect; set { if (_anchorRect == value) return; _anchorRect = value; QueueLayout(); } }
    public PopupSide PreferredSide { get => _side; set { if (_side == value) return; _side = value; QueueLayout(); } }
    public float Gap { get => _gap; set { if (_gap == value) return; _gap = value; QueueLayout(); } }
    public bool StayInSafeArea { get; set; } = true;

    public void Popup(Rect2 anchor, PopupSide side)
    {
        AnchorRect = anchor;
        PreferredSide = side;
        Visible = true;
        MoveToFront();
    }

    protected internal override Rect2 ComputeAnchoredRect(Rect2 parent)
    {
        var bounds = parent;
        if (StayInSafeArea && Root != null) bounds = bounds.Intersection(Root.SafeRect);
        var size = Vector2.ComponentMin(GetCombinedMinimumSize(), MaximumOrInfinite());
        return PopupPlacement.Place(_anchorRect, size, bounds, _side, _gap);
    }

    private Vector2 MaximumOrInfinite()
    {
        var max = CustomMaximumSize;
        return new Vector2(max.X > 0f ? max.X : float.MaxValue, max.Y > 0f ? max.Y : float.MaxValue);
    }
}
