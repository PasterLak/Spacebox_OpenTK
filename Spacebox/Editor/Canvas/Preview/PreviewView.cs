using Engine.CanvasUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Editor.Preview;

namespace Spacebox.Editor.Canvas.Preview;

public sealed class PreviewView : Control
{
    private const float ClickTolerance = 4f;

    private readonly PreviewViewport _viewport;
    private readonly TextureHandleRect _image = new TextureHandleRect { MouseFilter = MouseFilter.Ignore };
    private readonly Label _hint = new Label { FontColor = new Color4(1f, 1f, 1f, 0.7f), MouseFilter = MouseFilter.Ignore };
    private Vector2 _pressPosition;
    private Vector2 _lastPosition;
    private bool _dragged;

    public PreviewView(PreviewViewport viewport)
    {
        _viewport = viewport;
        ClipContents = true;
        CustomMinimumSize = new Vector2(120f, 120f);
        _image.Handle = () => _viewport.TextureHandle;
        AddChild(_image);
        _image.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_hint);
        _hint.Position = new Vector2(6f, 4f);
    }

    public PreviewViewport Viewport => _viewport;

    public override void Process(float deltaTime)
    {
        _hint.Text = _viewport.Hint;
        float scale = Root?.Scale ?? 1f;
        _viewport.Present((int)(Size.X * scale), (int)(Size.Y * scale));
    }

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button == MouseButton.Right)
        {
            _viewport.ToggleAutoRotate();
            return true;
        }

        if (button != MouseButton.Left) return false;
        _pressPosition = _lastPosition = localPosition;
        _dragged = false;
        if ((Root?.ClickCount ?? 1) >= 2 && _viewport.ClickTarget == null) _viewport.ResetView();
        return true;
    }

    protected override void OnMouseMove(Vector2 localPosition, bool pressed)
    {
        if (!pressed || !IsMouseDown) return;
        var delta = localPosition - _lastPosition;
        _lastPosition = localPosition;
        if ((localPosition - _pressPosition).Length > ClickTolerance) _dragged = true;
        if (_dragged) _viewport.Orbit(delta.X, delta.Y);
    }

    protected override bool OnMouseUp(Vector2 localPosition, MouseButton button, bool inside)
    {
        if (button != MouseButton.Left) return true;
        if (!_dragged && inside && Size.X > 0f && Size.Y > 0f) _viewport.ClickAt(localPosition.X / Size.X, localPosition.Y / Size.Y);
        return true;
    }

    protected override bool OnScroll(Vector2 localPosition, float delta)
    {
        _viewport.Zoom(delta);
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        renderer.DrawRect(LocalRect, new Color4(0.04f, 0.045f, 0.055f, 1f));
    }
}
