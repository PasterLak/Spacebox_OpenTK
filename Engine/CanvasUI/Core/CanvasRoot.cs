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

public readonly struct KeyboardFrame
{
    public static readonly KeyboardFrame Empty = new KeyboardFrame(Array.Empty<KeyPress>(), "");

    public KeyboardFrame(IReadOnlyList<KeyPress> keys, string text)
    {
        Keys = keys ?? Array.Empty<KeyPress>();
        Text = text ?? "";
    }

    public IReadOnlyList<KeyPress> Keys { get; }
    public string Text { get; }
}

public interface ICanvasClipboard
{
    string GetText();
    void SetText(string text);
}

public sealed class MemoryClipboard : ICanvasClipboard
{
    private string _text = "";

    public string GetText() => _text;

    public void SetText(string text) => _text = text ?? "";
}

public sealed class CanvasRoot
{
    private const float TooltipDelay = 0.45f;
    private const float DoubleClickTime = 0.35f;
    private const float DoubleClickDistance = 4f;

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
    private float _time;
    private float _lastClickTime = -10f;
    private Vector2 _lastClickPosition;
    private Control _focus;

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
    public bool WantsKeyboard => _focus != null;
    public Control FocusOwner => _focus;
    public int ClickCount { get; private set; }
    public ICanvasClipboard Clipboard { get; set; } = new MemoryClipboard();
    public event Action<KeyPress> UnhandledKeyPress;
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

    public static UiLayer PopupLayerFor(CanvasItem owner)
    {
        var layer = owner == null ? null : UiLayerRoot.LayerOf(owner);
        return layer.HasValue && layer.Value >= UiLayer.Modal ? UiLayer.ModalPopup : UiLayer.Popup;
    }

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
        Update(deltaTime, pointer, KeyboardFrame.Empty);
    }

    public void Update(float deltaTime, PointerState pointer, KeyboardFrame keyboard)
    {
        _time += deltaTime;
        if (_layoutDirty) Layout();
        _content.ProcessTree(deltaTime);
        if (_layoutDirty) Layout();

        var previousPosition = PointerPosition;
        PointerPosition = ScreenToCanvas(pointer.Position);
        UpdateHover(deltaTime);
        if (PointerPosition != previousPosition) SendMouseMove();
        HandleButton(pointer.LeftDown, _previous.LeftDown, MouseButton.Left, ref _pressedLeft);
        HandleButton(pointer.RightDown, _previous.RightDown, MouseButton.Right, ref _pressedRight);
        if (pointer.Scroll != 0f) BubbleScroll(_hovered, pointer.Scroll);
        _previous = pointer;
        HandleKeyboard(keyboard);
        _tooltip.Update(this, TooltipText, PointerPosition);
    }

    public void SetFocus(Control control)
    {
        if (control != null && (control.Root != this || control.FocusMode == FocusMode.None)) control = null;
        if (_focus == control) return;

        var previous = _focus;
        _focus = control;
        previous?.NotifyFocusExited();
        _focus?.NotifyFocusEntered();
    }

    public void ReleaseFocus() => SetFocus(null);

    public bool FocusNext(bool backwards)
    {
        var focusable = new List<Control>();
        CollectFocusable(_content, focusable);
        if (focusable.Count == 0) return false;

        int index = _focus == null ? -1 : focusable.IndexOf(_focus);
        int next = backwards ? (index <= 0 ? focusable.Count - 1 : index - 1) : (index + 1) % focusable.Count;
        SetFocus(focusable[next]);
        return true;
    }

    private static void CollectFocusable(CanvasItem item, List<Control> result)
    {
        if (!item.Visible) return;
        if (item is Control control && control.FocusMode == FocusMode.All && control.CanTakeFocus) result.Add(control);
        var children = item.Children;
        for (int i = 0; i < children.Count; i++) CollectFocusable(children[i], result);
    }

    private void HandleKeyboard(KeyboardFrame keyboard)
    {
        if (_focus != null && (_focus.Root != this || !_focus.IsVisibleInTree)) SetFocus(null);

        var keys = keyboard.Keys;
        for (int i = 0; i < keys.Count; i++)
        {
            var press = keys[i];
            if (BubbleKey(_focus, press)) continue;
            if (press.Key == Keys.Tab && _focus != null && FocusNext(press.Shift)) continue;
            UnhandledKeyPress?.Invoke(press);
        }

        if (keyboard.Text.Length > 0) _focus?.OnTextInput(keyboard.Text);
    }

    private static bool BubbleKey(Control start, KeyPress press)
    {
        for (CanvasItem item = start; item != null; item = item.Parent)
            if (item is Control control && control.OnKeyPress(press)) return true;
        return false;
    }

    private void SendMouseMove()
    {
        var target = _pressedLeft ?? _hovered;
        target?.OnMouseMove(PointerPosition - target.Rect.Position, _pressedLeft != null);
    }

    private void UpdateFocusOnPress(Control hovered)
    {
        for (CanvasItem item = hovered; item != null; item = item.Parent)
        {
            if (item is Control control && control.FocusMode != FocusMode.None && control.CanTakeFocus)
            {
                SetFocus(control);
                return;
            }
        }
        SetFocus(null);
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
            if (button == MouseButton.Left) CountClick();
            HideAutoPopups(_hovered);
            UpdateFocusOnPress(_hovered);
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

    private void HideAutoPopups(Control target)
    {
        HideAutoPopups(target, UiLayer.Popup);
        HideAutoPopups(target, UiLayer.ModalPopup);
    }

    private void HideAutoPopups(Control target, UiLayer popupLayer)
    {
        if (!_layers.TryGetValue(popupLayer, out var layer)) return;

        var children = layer.Children;
        for (int i = children.Count - 1; i >= 0; i--)
        {
            if (children[i] is not PopupPanel { AutoHide: true, Visible: true } popup) continue;
            if (IsInside(target, popup) || (popup.Owner != null && IsInside(target, popup.Owner))) continue;
            popup.Hide();
        }
    }

    private static bool IsInside(CanvasItem item, CanvasItem ancestor)
    {
        for (var current = item; current != null; current = current.Parent)
            if (current == ancestor) return true;
        return false;
    }

    private void CountClick()
    {
        bool repeat = _time - _lastClickTime <= DoubleClickTime && (PointerPosition - _lastClickPosition).Length <= DoubleClickDistance;
        ClickCount = repeat ? ClickCount + 1 : 1;
        _lastClickTime = _time;
        _lastClickPosition = PointerPosition;
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
