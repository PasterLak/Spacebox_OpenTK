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
    private Vector2 _screenSize;
    private bool _layoutDirty = true;
    private PointerState _previous;
    private Control _hovered;
    private Control _pressedLeft;
    private Control _pressedRight;
    private float _hoverTime;

    public Vector2 VirtualSize { get; set; } = new Vector2(1280f, 720f);
    public StretchMode StretchMode { get; set; } = StretchMode.Expand;
    public Theme Theme { get; set; }
    public Control Content => _content;
    public Transform2D ScreenTransform { get; private set; } = Transform2D.Identity;
    public Vector2 ViewportSize { get; private set; }
    public float Scale { get; private set; } = 1f;
    public Control Hovered => _hovered;
    public Vector2 PointerPosition { get; private set; }
    public bool WantsPointer => _hovered != null || _pressedLeft != null || _pressedRight != null;
    public string TooltipText => _hovered != null && _hoverTime >= TooltipDelay && !string.IsNullOrEmpty(_hovered.TooltipText) ? _hovered.TooltipText : null;

    public CanvasRoot()
    {
        _content.SetAnchorsPreset(LayoutPreset.FullRect);
        _content.SetRoot(this);
    }

    public T Add<T>(T item) where T : CanvasItem => _content.AddChild(item);

    public void MarkLayoutDirty() => _layoutDirty = true;

    public void Resize(Vector2 screenSize)
    {
        if (screenSize == _screenSize && !_layoutDirty) return;
        if (screenSize != _screenSize)
        {
            _screenSize = screenSize;
            UpdateStretch();
        }
        Layout();
    }

    public void Layout()
    {
        _layoutDirty = false;
        _content.ApplyRect(new Rect2(Vector2.Zero, ViewportSize));
        if (_layoutDirty)
        {
            _layoutDirty = false;
            _content.ApplyRect(new Rect2(Vector2.Zero, ViewportSize));
        }
    }

    public Vector2 ScreenToCanvas(Vector2 screen) => ScreenTransform.Inverse().Apply(screen);

    public void Update(float deltaTime, PointerState pointer)
    {
        if (_layoutDirty) Layout();
        _content.ProcessTree(deltaTime);
        if (_layoutDirty) Layout();

        PointerPosition = ScreenToCanvas(pointer.Position);
        UpdateHover(deltaTime);
        HandleButton(pointer.LeftDown, _previous.LeftDown, MouseButton.Left, ref _pressedLeft);
        HandleButton(pointer.RightDown, _previous.RightDown, MouseButton.Right, ref _pressedRight);
        if (pointer.Scroll != 0f) Bubble(_hovered, c => c.OnScroll(PointerPosition - c.Rect.Position, pointer.Scroll));
        _previous = pointer;
    }

    public void Draw(ICanvasRenderer renderer)
    {
        if (_layoutDirty) Layout();
        _content.DrawTree(renderer, Color4.White);
    }

    public Control HitTest(Vector2 canvasPoint) => HitTest(_content, canvasPoint);

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

    private void UpdateStretch()
    {
        var screen = Vector2.ComponentMax(_screenSize, Vector2.One);
        switch (StretchMode)
        {
            case StretchMode.Disabled:
                Scale = 1f;
                ViewportSize = screen;
                ScreenTransform = Transform2D.Identity;
                break;
            case StretchMode.KeepAspect:
                Scale = MathF.Min(screen.X / VirtualSize.X, screen.Y / VirtualSize.Y);
                ViewportSize = VirtualSize;
                var offset = (screen - VirtualSize * Scale) * 0.5f;
                ScreenTransform = new Transform2D(new Vector2(Scale, 0f), new Vector2(0f, Scale), new Vector2(MathF.Floor(offset.X), MathF.Floor(offset.Y)));
                break;
            default:
                Scale = MathF.Min(screen.X / VirtualSize.X, screen.Y / VirtualSize.Y);
                ViewportSize = screen / Scale;
                ScreenTransform = Transform2D.Scaling(new Vector2(Scale));
                break;
        }
        _layoutDirty = true;
    }
}
