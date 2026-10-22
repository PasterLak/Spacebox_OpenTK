using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public class Control : CanvasItem
{
    private static readonly Dictionary<Type, string[]> _typeChains = new Dictionary<Type, string[]>();

    private float _anchorLeft, _anchorTop, _anchorRight, _anchorBottom;
    private float _offsetLeft, _offsetTop, _offsetRight, _offsetBottom;
    private Vector2 _customMinimumSize;
    private Vector2 _customMaximumSize;
    private SizeFlags _sizeFlagsHorizontal = SizeFlags.Fill;
    private SizeFlags _sizeFlagsVertical = SizeFlags.Fill;
    private float _stretchRatio = 1f;
    private string _themeTypeVariation;
    private string[] _themeTypes;
    private Dictionary<string, StyleBox> _styleOverrides;
    private Dictionary<string, Color4> _colorOverrides;
    private Dictionary<string, float> _constantOverrides;
    private float _fontSizeOverride;
    private Theme _theme;

    public Rect2 Rect { get; private set; }
    public Vector2 Size => Rect.Size;
    public Vector2 GlobalPosition => Rect.Position;
    public Rect2 LocalRect => new Rect2(Vector2.Zero, Rect.Size);
    public override Transform2D GlobalTransform => Transform2D.Translation(Rect.Position);

    public GrowDirection GrowHorizontal { get; set; } = GrowDirection.End;
    public GrowDirection GrowVertical { get; set; } = GrowDirection.End;
    public MouseFilter MouseFilter { get; set; } = MouseFilter.Stop;
    public bool ClipContents { get; set; }
    public string TooltipText { get; set; } = "";
    public bool IsHovered { get; internal set; }
    public bool IsMouseDown { get; internal set; }

    public event Action<Control> MouseEntered;
    public event Action<Control> MouseExited;
    public event Action<Control> Resized;

    public float AnchorLeft { get => _anchorLeft; set => SetLayoutValue(ref _anchorLeft, value); }
    public float AnchorTop { get => _anchorTop; set => SetLayoutValue(ref _anchorTop, value); }
    public float AnchorRight { get => _anchorRight; set => SetLayoutValue(ref _anchorRight, value); }
    public float AnchorBottom { get => _anchorBottom; set => SetLayoutValue(ref _anchorBottom, value); }
    public float OffsetLeft { get => _offsetLeft; set => SetLayoutValue(ref _offsetLeft, value); }
    public float OffsetTop { get => _offsetTop; set => SetLayoutValue(ref _offsetTop, value); }
    public float OffsetRight { get => _offsetRight; set => SetLayoutValue(ref _offsetRight, value); }
    public float OffsetBottom { get => _offsetBottom; set => SetLayoutValue(ref _offsetBottom, value); }
    public float SizeFlagsStretchRatio { get => _stretchRatio; set => SetLayoutValue(ref _stretchRatio, value); }

    public Vector2 CustomMinimumSize
    {
        get => _customMinimumSize;
        set
        {
            if (_customMinimumSize == value) return;
            _customMinimumSize = value;
            QueueLayout();
        }
    }

    public Vector2 CustomMaximumSize
    {
        get => _customMaximumSize;
        set
        {
            if (_customMaximumSize == value) return;
            _customMaximumSize = value;
            QueueLayout();
        }
    }

    public virtual bool IsInteractive => false;

    public SizeFlags SizeFlagsHorizontal
    {
        get => _sizeFlagsHorizontal;
        set
        {
            _sizeFlagsHorizontal = value;
            QueueLayout();
        }
    }

    public SizeFlags SizeFlagsVertical
    {
        get => _sizeFlagsVertical;
        set
        {
            _sizeFlagsVertical = value;
            QueueLayout();
        }
    }

    public Theme Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            QueueLayout();
        }
    }

    public string ThemeTypeVariation
    {
        get => _themeTypeVariation;
        set
        {
            _themeTypeVariation = value;
            _themeTypes = null;
            QueueLayout();
        }
    }

    public Vector2 Position
    {
        get => new Vector2(_offsetLeft, _offsetTop);
        set
        {
            var size = new Vector2(_offsetRight - _offsetLeft, _offsetBottom - _offsetTop);
            _offsetLeft = value.X;
            _offsetTop = value.Y;
            _offsetRight = value.X + size.X;
            _offsetBottom = value.Y + size.Y;
            QueueLayout();
        }
    }

    public Vector2 RequestedSize
    {
        get => new Vector2(_offsetRight - _offsetLeft, _offsetBottom - _offsetTop);
        set
        {
            _offsetRight = _offsetLeft + value.X;
            _offsetBottom = _offsetTop + value.Y;
            QueueLayout();
        }
    }

    public virtual Vector2 GetMinimumSize() => Vector2.Zero;

    public Vector2 GetCombinedMinimumSize() => Vector2.ComponentMax(_customMinimumSize, GetMinimumSize());

    public Control SetAnchors(float left, float top, float right, float bottom)
    {
        _anchorLeft = left;
        _anchorTop = top;
        _anchorRight = right;
        _anchorBottom = bottom;
        QueueLayout();
        return this;
    }

    public Control SetOffsets(float left, float top, float right, float bottom)
    {
        _offsetLeft = left;
        _offsetTop = top;
        _offsetRight = right;
        _offsetBottom = bottom;
        QueueLayout();
        return this;
    }

    public Control SetAnchorsPreset(LayoutPreset preset)
    {
        var (l, t, r, b) = LayoutPresets.Anchors(preset);
        return SetAnchors(l, t, r, b);
    }

    public Control SetAnchorsAndOffsetsPreset(LayoutPreset preset, float margin = 0f, Vector2? size = null)
    {
        SetAnchorsPreset(preset);
        var s = size ?? GetCombinedMinimumSize();
        var (l, t, r, b) = LayoutPresets.Offsets(preset, s, margin);
        GrowHorizontal = LayoutPresets.GrowHorizontal(preset);
        GrowVertical = LayoutPresets.GrowVertical(preset);
        return SetOffsets(l, t, r, b);
    }

    public void AddThemeStyleBoxOverride(string name, StyleBox style)
    {
        (_styleOverrides ??= new Dictionary<string, StyleBox>())[name] = style;
        QueueLayout();
    }

    public void AddThemeColorOverride(string name, Color4 color)
    {
        (_colorOverrides ??= new Dictionary<string, Color4>())[name] = color;
    }

    public void AddThemeConstantOverride(string name, float value)
    {
        (_constantOverrides ??= new Dictionary<string, float>())[name] = value;
        QueueLayout();
    }

    public void AddThemeFontSizeOverride(float size)
    {
        _fontSizeOverride = size;
        QueueLayout();
    }

    public StyleBox GetThemeStyleBox(string name)
    {
        if (_styleOverrides != null && _styleOverrides.TryGetValue(name, out var style)) return style;
        var types = ThemeTypes();
        CanvasItem cursor = this;
        int stage = 0;
        for (var theme = NextTheme(ref cursor, ref stage); theme != null; theme = NextTheme(ref cursor, ref stage))
        {
            for (int i = 0; i < types.Length; i++)
                if (theme.TryGetStyleBox(types[i], name, out style)) return style;
        }
        return null;
    }

    public Color4 GetThemeColor(string name, Color4 fallback)
    {
        if (_colorOverrides != null && _colorOverrides.TryGetValue(name, out var color)) return color;
        var types = ThemeTypes();
        CanvasItem cursor = this;
        int stage = 0;
        for (var theme = NextTheme(ref cursor, ref stage); theme != null; theme = NextTheme(ref cursor, ref stage))
        {
            for (int i = 0; i < types.Length; i++)
                if (theme.TryGetColor(types[i], name, out color)) return color;
        }
        return fallback;
    }

    public float GetThemeConstant(string name, float fallback)
    {
        if (_constantOverrides != null && _constantOverrides.TryGetValue(name, out var value)) return value;
        var types = ThemeTypes();
        CanvasItem cursor = this;
        int stage = 0;
        for (var theme = NextTheme(ref cursor, ref stage); theme != null; theme = NextTheme(ref cursor, ref stage))
        {
            for (int i = 0; i < types.Length; i++)
                if (theme.TryGetConstant(types[i], name, out value)) return value;
        }
        return fallback;
    }

    public CanvasFont GetThemeFont()
    {
        CanvasItem cursor = this;
        int stage = 0;
        for (var theme = NextTheme(ref cursor, ref stage); theme != null; theme = NextTheme(ref cursor, ref stage))
            if (theme.DefaultFont != null) return theme.DefaultFont;
        return null;
    }

    public float GetThemeFontSize()
    {
        if (_fontSizeOverride > 0f) return _fontSizeOverride;
        for (CanvasItem item = this; item != null; item = item.Parent)
            if (item is Control control && control._fontSizeOverride > 0f && control != this) return control._fontSizeOverride;
        CanvasItem cursor = this;
        int stage = 0;
        return NextTheme(ref cursor, ref stage)?.DefaultFontSize ?? Theme.Fallback.DefaultFontSize;
    }

    public bool HasPoint(Vector2 globalPoint) => Rect.Contains(globalPoint);

    protected internal virtual void OnMouseEnter()
    {
        MouseEntered?.Invoke(this);
    }

    protected internal virtual void OnMouseExit()
    {
        MouseExited?.Invoke(this);
    }

    protected internal virtual bool OnMouseDown(Vector2 localPosition, MouseButton button) => false;

    protected internal virtual bool OnMouseUp(Vector2 localPosition, MouseButton button, bool inside) => false;

    protected internal virtual bool OnScroll(Vector2 localPosition, float delta) => false;

    internal void ApplyRect(Rect2 rect)
    {
        bool resized = rect.Size != Rect.Size;
        Rect = rect;
        LayoutChildren();
        if (!resized) return;
        OnResized();
        Resized?.Invoke(this);
    }

    protected virtual void OnResized()
    {
    }

    protected virtual void LayoutChildren()
    {
        var children = Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is Control child && child.Visible) child.ApplyRect(child.ComputeAnchoredRect(Rect));
            else if (children[i] is not Control) LayoutNested(children[i]);
        }
    }

    internal static void LayoutNested(CanvasItem item)
    {
        var children = item.Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is Control control && control.Visible)
            {
                var origin = item.GlobalTransform.Origin;
                control.ApplyRect(control.ComputeAnchoredRect(new Rect2(origin, Vector2.Zero)));
            }
            else if (children[i] is not Control) LayoutNested(children[i]);
        }
    }

    protected internal virtual Rect2 ComputeAnchoredRect(Rect2 parent)
    {
        float left = parent.X + _anchorLeft * parent.Width + _offsetLeft;
        float right = parent.X + _anchorRight * parent.Width + _offsetRight;
        float top = parent.Y + _anchorTop * parent.Height + _offsetTop;
        float bottom = parent.Y + _anchorBottom * parent.Height + _offsetBottom;

        var min = GetCombinedMinimumSize();
        Grow(ref left, ref right, min.X, GrowHorizontal);
        Grow(ref top, ref bottom, min.Y, GrowVertical);
        if (_customMaximumSize.X > 0f) Limit(ref left, ref right, MathF.Max(_customMaximumSize.X, min.X), GrowHorizontal);
        if (_customMaximumSize.Y > 0f) Limit(ref top, ref bottom, MathF.Max(_customMaximumSize.Y, min.Y), GrowVertical);
        float x = MathF.Round(left);
        float y = MathF.Round(top);
        return new Rect2(x, y, MathF.Round(right - left), MathF.Round(bottom - top));
    }

    internal override void DrawTree(ICanvasRenderer renderer, Color4 parentModulate)
    {
        if (!Visible) return;

        var modulate = Multiply(parentModulate, Modulate);
        renderer.Transform = GlobalTransform;
        renderer.Modulate = Multiply(modulate, SelfModulate);
        Draw(renderer);

        if (!ClipContents)
        {
            DrawChildren(renderer, modulate);
            return;
        }

        renderer.Transform = Transform2D.Identity;
        renderer.PushClip(Rect);
        DrawChildren(renderer, modulate);
        renderer.PopClip();
    }

    private static void Grow(ref float begin, ref float end, float min, GrowDirection direction)
    {
        float size = end - begin;
        if (size >= min) return;

        switch (direction)
        {
            case GrowDirection.Begin:
                begin = end - min;
                break;
            case GrowDirection.Both:
                float center = (begin + end) * 0.5f;
                begin = center - min * 0.5f;
                end = center + min * 0.5f;
                break;
            default:
                end = begin + min;
                break;
        }
    }

    private static void Limit(ref float begin, ref float end, float max, GrowDirection direction)
    {
        if (end - begin <= max) return;
        switch (direction)
        {
            case GrowDirection.Begin:
                begin = end - max;
                break;
            case GrowDirection.Both:
                float center = (begin + end) * 0.5f;
                begin = center - max * 0.5f;
                end = center + max * 0.5f;
                break;
            default:
                end = begin + max;
                break;
        }
    }

    private void SetLayoutValue(ref float field, float value)
    {
        if (field == value) return;
        field = value;
        QueueLayout();
    }

    private string[] ThemeTypes()
    {
        if (_themeTypes != null) return _themeTypes;
        var chain = TypeChain(GetType());
        if (string.IsNullOrEmpty(_themeTypeVariation)) return _themeTypes = chain;

        var types = new string[chain.Length + 1];
        types[0] = _themeTypeVariation;
        Array.Copy(chain, 0, types, 1, chain.Length);
        return _themeTypes = types;
    }

    private static string[] TypeChain(Type type)
    {
        if (_typeChains.TryGetValue(type, out var chain)) return chain;
        var names = new List<string>();
        for (var t = type; t != null && t != typeof(CanvasItem); t = t.BaseType) names.Add(t.Name);
        chain = names.ToArray();
        _typeChains[type] = chain;
        return chain;
    }

    private Theme NextTheme(ref CanvasItem cursor, ref int stage)
    {
        while (stage == 0)
        {
            if (cursor == null)
            {
                stage = 1;
                break;
            }
            var item = cursor;
            cursor = cursor.Parent;
            if (item is Control control && control._theme != null) return control._theme;
        }

        if (stage == 1)
        {
            stage = 2;
            if (Root?.Theme != null) return Root.Theme;
        }

        if (stage == 2)
        {
            stage = 3;
            return Theme.Fallback;
        }

        return null;
    }
}
