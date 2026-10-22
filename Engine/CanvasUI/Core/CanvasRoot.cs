using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public struct PointerState
{
    public Vector2 Position;
    public bool LeftDown;
    public bool RightDown;
    public float Scroll;
}

public sealed class CanvasRoot
{
    private const float TooltipDelay = 0.45f;

    private readonly Control _content = new Control { Name = "Root", MouseFilter = MouseFilter.Ignore };
    private readonly Dictionary<UiLayer, UiLayerRoot> _layers = new Dictionary<UiLayer, UiLayerRoot>();
    private readonly TooltipPresenter _tooltip = new TooltipPresenter();
    private Vector2 _screenSize;
    private StretchResult _stretch;
    private bool _hasStretch;
    private bool _layoutDirty = true;
    private PointerState _previous;
    private Control _hovered;
    private Control _pressedLeft;
    private Control _pressedRight;
    private float _hoverTime;

    public StretchSettings Stretch { get; } = new StretchSettings();
    public Vector2 VirtualSize { get => Stretch.VirtualSize; set => Stretch.VirtualSize = value; }
    public StretchMode StretchMode { get => Stretch.Mode; set => Stretch.Mode = value; }
    public Theme Theme { get; set; }
    public Control Content => _content;
    public Transform2D ScreenTransform { get; private set; } = Transform2D.Identity;
    public Vector2 ScreenSize => _screenSize;
    public Vector2 ViewportSize { get; private set; }
    public Rect2 ViewportRect => new Rect2(Vector2.Zero, ViewportSize);
    public Rect2 SafeRect { get; private set; }
    public float Scale { get; private set; } = 1f;
    public Control Hovered => _hovered;
    public Vector2 PointerPosition { get; private set; }
    public bool WantsPointer => _hovered != null || _pressedLeft != null || _pressedRight != null;
    public bool IsLayoutDirty => _layoutDirty;
    public PopupPanel TooltipPanel => _tooltip.Panel;
    public string TooltipText => _hovered != null && _hoverTime >= TooltipDelay && !string.IsNullOrEmpty(_hovered.TooltipText) ? _hovered.TooltipText : null;

    public CanvasRoot()
    {
        _content.SetAnchorsPreset(LayoutPreset.FullRect);
        _content.SetRoot(this);
    }

    public T Add<T>(T item, UiLayer layer = UiLayer.Hud) where T : CanvasItem => Layer(layer).AddChild(item);

    public UiLayerRoot Layer(UiLayer layer)
    {
        if (_layers.TryGetValue(layer, out var root)) return root;

        root = new UiLayerRoot(layer);
        int index = 0;
        var children = _content.Children;
        while (index < children.Count && (children[index] is not UiLayerRoot other || other.Layer < layer)) index++;
        _content.InsertChild(index, root);
        _layers[layer] = root;
        return root;
    }

    public bool TryGetLayer(UiLayer layer, out UiLayerRoot root) => _layers.TryGetValue(layer, out root);

    public void MarkLayoutDirty() => _layoutDirty = true;

    public void Resize(Vector2 screenSize)
    {
        _screenSize = screenSize;
        var stretch = CanvasStretch.Compute(Stretch, screenSize);
        if (!_hasStretch || !stretch.Equals(_stretch)) ApplyStretch(stretch);
        if (_layoutDirty) Layout();
    }

    public void EnsureLayout()
    {
        if (_layoutDirty) Layout();
    }

    public void Layout()
    {
        if (!_hasStretch) ApplyStretch(CanvasStretch.Compute(Stretch, _screenSize));
        for (int pass = 0; pass < 3; pass++)
        {
            _layoutDirty = false;
            _content.ApplyRect(new Rect2(Vector2.Zero, ViewportSize));
            if (!_layoutDirty) return;
        }
    }

    public Vector2 ScreenToCanvas(Vector2 screen) => ScreenTransform.Inverse().Apply(screen);

    public Vector2 CanvasToScreen(Vector2 canvas) => ScreenTransform.Apply(canvas);

    public void Update(float deltaTime, PointerState pointer)
    {
        if (_layoutDirty) Layout();
        _content.ProcessTree(deltaTime);
        if (_layoutDirty) Layout();

        PointerPosition = ScreenToCanvas(pointer.Position);
        UpdateHover(deltaTime);
        HandleButton(pointer.LeftDown, _previous.LeftDown, MouseButton.Left, ref _pressedLeft);
        HandleButton(pointer.RightDown, _previous.RightDown, MouseButton.Right, ref _pressedRight);
        if (pointer.Scroll != 0f) BubbleScroll(_hovered, pointer.Scroll);
        _previous = pointer;
        _tooltip.Update(this, TooltipText, PointerPosition);
    }

    public void Draw(ICanvasRenderer renderer)
    {
        if (_layoutDirty) Layout();
        _content.DrawTree(renderer, Color4.White);
    }

    public Control HitTest(Vector2 canvasPoint)
    {
        var children = _content.Children;
        for (int i = children.Count - 1; i >= 0; i--)
        {
            var hit = HitTest(children[i], canvasPoint);
            if (hit != null) return hit;
            if (children[i] is UiLayerRoot { Layer: UiLayer.Modal } modal && modal.Visible && modal.HasVisibleContent) return null;
        }
        return null;
    }

    public bool IsBlockedByModal(CanvasItem item)
    {
        var layer = UiLayerRoot.LayerOf(item);
        if (layer == null || layer >= UiLayer.Modal) return false;
        return _layers.TryGetValue(UiLayer.Modal, out var modal) && modal.Visible && modal.HasVisibleContent;
    }

    private static Control HitTest(CanvasItem item, Vector2 point)
    {
        if (!item.Visible) return null;
        var control = item as Control;
        if (control != null && control.ClipContents && !control.HasPoint(point)) return null;

        var children = item.Children;
        for (int i = children.Count - 1; i >= 0; i--)
        {
            var hit = HitTest(children[i], point);
            if (hit != null) return hit;
        }

        if (control != null && control.MouseFilter != MouseFilter.Ignore && control.HasPoint(point)) return control;
        return null;
    }

    private void UpdateHover(float deltaTime)
    {
        var hovered = HitTest(PointerPosition);
        if (hovered != _hovered)
        {
            if (_hovered != null)
            {
                _hovered.IsHovered = false;
                _hovered.OnMouseExit();
            }
            _hovered = hovered;
            _hoverTime = 0f;
            if (_hovered != null)
            {
                _hovered.IsHovered = true;
                _hovered.OnMouseEnter();
            }
        }
        else
        {
            _hoverTime += deltaTime;
        }
    }

    private void HandleButton(bool down, bool wasDown, MouseButton button, ref Control pressed)
    {
        if (down && !wasDown)
        {
            pressed = Bubble(_hovered, c => c.OnMouseDown(PointerPosition - c.Rect.Position, button));
            if (pressed != null) pressed.IsMouseDown = true;
            _hoverTime = 0f;
        }
        else if (!down && wasDown && pressed != null)
        {
            var target = pressed;
            pressed = null;
            target.IsMouseDown = false;
            target.OnMouseUp(PointerPosition - target.Rect.Position, button, target.HasPoint(PointerPosition) && IsInsideClip(target));
        }
    }

    private bool IsInsideClip(Control control)
    {
        for (CanvasItem item = control.Parent; item != null; item = item.Parent)
            if (item is Control parent && parent.ClipContents && !parent.HasPoint(PointerPosition)) return false;
        return true;
    }

    private static Control Bubble(Control start, Func<Control, bool> handler)
    {
        for (CanvasItem item = start; item != null; item = item.Parent)
        {
            if (item is not Control control || control.MouseFilter == MouseFilter.Ignore) continue;
            if (handler(control)) return control;
            if (control.MouseFilter == MouseFilter.Stop) return null;
        }
        return null;
    }

    private void BubbleScroll(Control start, float delta)
    {
        for (CanvasItem item = start; item != null; item = item.Parent)
            if (item is Control control && control.OnScroll(PointerPosition - control.Rect.Position, delta)) return;
    }

    private void ApplyStretch(StretchResult stretch)
    {
        _stretch = stretch;
        _hasStretch = true;
        Scale = stretch.Scale;
        ViewportSize = stretch.ViewportSize;
        ScreenTransform = stretch.ScreenTransform;
        SafeRect = stretch.SafeRect;
        _layoutDirty = true;
    }
}
