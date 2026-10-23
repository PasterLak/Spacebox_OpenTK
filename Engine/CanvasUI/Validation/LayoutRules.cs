namespace Engine.CanvasUI.Validation;

public sealed class LayoutRules
{
    private readonly HashSet<Control> _ignored = new HashSet<Control>();
    private readonly HashSet<(Control, Control)> _allowedOverlaps = new HashSet<(Control, Control)>();
    private readonly HashSet<(UiLayer Upper, UiLayer Lower)> _forbiddenCovers = new HashSet<(UiLayer, UiLayer)> { (UiLayer.Windows, UiLayer.Hud) };
    private readonly HashSet<Type> _transparentTypes = new HashSet<Type> { typeof(Control) };
    private readonly HashSet<UiLayer> _transientLayers = new HashSet<UiLayer> { UiLayer.Popup, UiLayer.ModalPopup, UiLayer.Tooltip };

    public float Tolerance { get; set; } = 0.5f;
    public float MinHitTargetPixels { get; set; } = 24f;
    public float MinFontPixels { get; set; } = 9f;
    public bool RequireSafeArea { get; set; } = true;
    public List<ILayoutCheck> Checks { get; } = DefaultChecks();

    public IReadOnlyCollection<(UiLayer Upper, UiLayer Lower)> ForbiddenCovers => _forbiddenCovers;

    public static List<ILayoutCheck> DefaultChecks() => new List<ILayoutCheck>
    {
        new BoundsCheck(),
        new OverflowCheck(),
        new RegionOverlapCheck(),
        new TextCheck(),
        new HitTargetCheck(),
        new PopupAnchorCheck()
    };

    public LayoutRules Ignore(Control control)
    {
        if (control != null) _ignored.Add(control);
        return this;
    }

    public LayoutRules AllowOverlap(Control a, Control b)
    {
        _allowedOverlaps.Add((a, b));
        _allowedOverlaps.Add((b, a));
        return this;
    }

    public LayoutRules ForbidCover(UiLayer upper, UiLayer lower)
    {
        _forbiddenCovers.Add((upper, lower));
        return this;
    }

    public LayoutRules AllowCover(UiLayer upper, UiLayer lower)
    {
        _forbiddenCovers.Remove((upper, lower));
        return this;
    }

    public LayoutRules TreatAsTransparent(Type type)
    {
        _transparentTypes.Add(type);
        return this;
    }

    public LayoutRules TreatAsTransient(UiLayer layer)
    {
        _transientLayers.Add(layer);
        return this;
    }

    public bool IsTransient(UiLayer? layer) => layer.HasValue && _transientLayers.Contains(layer.Value);

    public bool IsIgnored(CanvasItem item)
    {
        for (var cursor = item; cursor != null; cursor = cursor.Parent)
            if (cursor is Control control && _ignored.Contains(control)) return true;
        return false;
    }

    public bool IsOverlapAllowed(Control a, Control b) => _allowedOverlaps.Contains((a, b));

    public bool IsTransparent(Control control) => control is UiLayerRoot || control is AreaControl || _transparentTypes.Contains(control.GetType());
}
