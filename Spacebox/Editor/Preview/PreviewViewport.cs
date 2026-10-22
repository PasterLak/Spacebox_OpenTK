using System.Numerics;
using Engine;
using Engine.Graphics;

namespace Spacebox.Editor.Preview;

public sealed class PreviewViewport : IDisposable
{
    private const float OrbitSpeed = 0.4f;
    private const float AutoRotateSpeed = 18f;
    private const int MaxCaptureSize = 4096;

    private readonly RenderTexture _target = new RenderTexture(256, 256);
    private readonly TrackballViewCamera _camera = new TrackballViewCamera();
    private IPreviewContent _content;
    private object _key;
    private int _assetsVersion = -1;
    private bool _visible;
    private bool _framed;
    private bool _autoRotate = true;
    private string _capturePath;
    private float _captureScale = 1f;

    public IPreviewContent Content => _content;
    public bool AutoRotate { get => _autoRotate; set => _autoRotate = value; }
    public Action Background { get; set; }

    public void SetContent(object key, int assetsVersion, Func<IPreviewContent> factory)
    {
        if (Equals(key, _key) && assetsVersion == _assetsVersion && _content != null) return;

        _content?.Dispose();
        _content = null;
        _key = key;
        _assetsVersion = assetsVersion;
        _framed = false;

        try
        {
            _content = factory?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.Error($"[Editor] Preview failed: {ex.Message}");
        }
    }

    public void Clear()
    {
        _content?.Dispose();
        _content = null;
        _key = null;
    }

    public int TextureHandle => _target.ColorTexture;

    public string Hint
    {
        get
        {
            if (_content == null) return "No preview";
            if (!_content.IsReady) return "Generating...";
            return ClickTarget != null ? "Click: shoot  Drag: rotate  Wheel: zoom  RMB: auto rotate" : "Drag: rotate  Wheel: zoom  Double click: reset  RMB: auto rotate";
        }
    }

    public IPreviewClickTarget ClickTarget => _content is IPreviewClickTarget { AcceptsClicks: true } target && _content.IsReady ? target : null;

    public void Present(int width, int height)
    {
        width = Math.Max(32, width);
        height = Math.Max(32, height);
        _visible = true;
        _target.Resize(width, height);
        float aspect = width / (float)height;
        if (MathF.Abs(aspect - _camera.AspectRatio) <= 0.01f) return;
        _camera.AspectRatio = aspect;
        if (_framed) Reframe();
    }

    public void Orbit(float dx, float dy)
    {
        _camera.Orbit(-dx * OrbitSpeed, dy * OrbitSpeed);
        _autoRotate = false;
    }

    public void Zoom(float wheel) => _camera.Zoom(wheel);

    public void ToggleAutoRotate() => _autoRotate = !_autoRotate;

    public void ClickAt(float x01, float y01)
    {
        ClickTarget?.Click(_camera.Position, _camera.ViewportPointToDirection(new OpenTK.Mathematics.Vector2(x01, y01)));
    }

    public void Update(float deltaTime)
    {
        if (_content == null) return;

        _content.Update(deltaTime);
        if (_content.IsReady && !_framed) Reframe();
        if (_autoRotate) _camera.Spin(OpenTK.Mathematics.Vector3.UnitY, AutoRotateSpeed * deltaTime);
    }

    public void RequestCapture(string path, float scale)
    {
        _capturePath = path;
        _captureScale = MathF.Max(1f, scale);
    }

    public void Render()
    {
        Capture();
        if (!_visible) return;
        _visible = false;

        RenderTo(_target);
    }

    private void RenderTo(RenderTexture target)
    {
        var content = _content;
        var background = Background;
        target.Render(_camera, () =>
        {
            background?.Invoke();
            if (content != null && content.IsReady) content.Render();
        });
    }

    private void Capture()
    {
        if (_capturePath == null) return;

        string path = _capturePath;
        _capturePath = null;
        if (_content == null || !_content.IsReady) return;

        float scale = MathF.Min(_captureScale, MaxCaptureSize / (float)Math.Max(_target.Width, _target.Height));
        using var capture = new RenderTexture((int)(_target.Width * scale), (int)(_target.Height * scale));
        try
        {
            RenderTo(capture);
            FramebufferCapture.SaveRenderTexture(capture, path);
            Debug.Success($"Preview picture saved: {Path.GetFullPath(path)}");
        }
        catch (Exception ex)
        {
            Debug.Error($"[Editor] Preview picture could not be saved: {ex.Message}");
        }
        finally
        {
            _camera.AspectRatio = (float)_target.Width / _target.Height;
        }
    }

    public void ResetView()
    {
        _framed = false;
        Reframe();
    }

    public void Reframe()
    {
        if (_content == null || !_content.IsReady) return;
        if (!_framed) _camera.SetAngles(_content.ViewAngles.X, _content.ViewAngles.Y);
        _camera.Frame(_content.Center, _content.Radius);
        _framed = true;
    }

    public void Dispose()
    {
        _content?.Dispose();
        _content = null;
        _target.Dispose();
    }
}
