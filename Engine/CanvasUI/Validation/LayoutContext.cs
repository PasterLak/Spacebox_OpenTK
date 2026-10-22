namespace Engine.CanvasUI.Validation;

public readonly struct LayoutEntry
{
    public readonly Control Control;
    public readonly Rect2 ClipRect;
    public readonly bool IsClipped;
    public readonly UiLayer? Layer;

    public LayoutEntry(Control control, Rect2 clipRect, bool isClipped, UiLayer? layer)
    {
        Control = control;
        ClipRect = clipRect;
        IsClipped = isClipped;
        Layer = layer;
    }

    public Rect2 VisibleRect => Control.Rect.Intersection(ClipRect);
}

public sealed class LayoutContext
{
    private readonly List<LayoutEntry> _entries = new List<LayoutEntry>();
    private readonly Dictionary<UiLayer, List<Control>> _regions = new Dictionary<UiLayer, List<Control>>();
    private readonly Dictionary<Control, int> _index = new Dictionary<Control, int>();

    public CanvasRoot Root { get; }
    public LayoutRules Rules { get; }
    public IReadOnlyList<LayoutEntry> Entries => _entries;
    public IReadOnlyDictionary<UiLayer, List<Control>> Regions => _regions;
    public Rect2 Viewport => Root.ViewportRect;
    public Rect2 SafeRect => Root.SafeRect;

    public LayoutContext(CanvasRoot root, LayoutRules rules)
    {
        Root = root;
        Rules = rules;
        root.EnsureLayout();
        Collect(root.Content, root.ViewportRect, false, null);
        CollectRegions();
    }

    public bool TryGetEntry(Control control, out LayoutEntry entry)
    {
        if (_index.TryGetValue(control, out int i))
        {
            entry = _entries[i];
            return true;
        }
        entry = default;
        return false;
    }

    public bool IsTransparent(Control control) => Rules.IsTransparent(control);

    private void Collect(CanvasItem item, Rect2 clip, bool clipped, UiLayer? layer)
    {
        if (!item.Visible) return;
        var control = item as Control;
        if (control is UiLayerRoot layerRoot) layer = layerRoot.Layer;

        if (control != null)
        {
            _index[control] = _entries.Count;
            _entries.Add(new LayoutEntry(control, clip, clipped, layer));
            if (control.ClipContents)
            {
                clip = clip.Intersection(control.Rect);
                clipped = true;
            }
        }

        var children = item.Children;
        for (int i = 0; i < children.Count; i++) Collect(children[i], clip, clipped, layer);
    }

    private void CollectRegions()
    {
        var layers = Root.Content.Children;
        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i] is not UiLayerRoot layer || !layer.Visible) continue;
            var list = new List<Control>();
            CollectRegions(layer, list);
            _regions[layer.Layer] = list;
        }
    }

    private void CollectRegions(Control parent, List<Control> regions)
    {
        var children = parent.Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is not Control child || !child.Visible || Rules.IsIgnored(child)) continue;
            if (IsTransparent(child)) CollectRegions(child, regions);
            else if (child.Rect.HasArea) regions.Add(child);
        }
    }
}
