using System.Numerics;
using Engine;
using Engine.Graphics;
using ImGuiNET;

namespace Spacebox.Editor.Preview;

public sealed class PreviewViewport : IDisposable
{
    private const float OrbitSpeed = 0.4f;
    private const float AutoRotateSpeed = 18f;

    private readonly RenderTexture _target = new RenderTexture(256, 256);
    private readonly TrackballViewCamera _camera = new TrackballViewCamera();
    private IPreviewContent _content;
    private object _key;
    private int _assetsVersion = -1;
    private bool _visible;
    private bool _framed;
    private bool _autoRotate = true;

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

    public void Draw(Vector2 size)
    {
        size = Vector2.Max(size, new Vector2(32f, 32f));
        _visible = true;
        _target.Resize((int)size.X, (int)size.Y);
        float aspect = size.X / size.Y;
        if (MathF.Abs(aspect - _camera.AspectRatio) > 0.01f)
        {
            _camera.AspectRatio = aspect;
            if (_framed) Reframe();
        }

        var start = ImGui.GetCursorScreenPos();
        ImGui.Image(_target.ColorTexture, size, new Vector2(0, 1), new Vector2(1, 0));
        ImGui.SetCursorScreenPos(start);
        ImGui.InvisibleButton("##previewInput", size);

        bool hovered = ImGui.IsItemHovered();
        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left, 0f))
        {
            var delta = ImGui.GetIO().MouseDelta;
            _camera.Orbit(-delta.X * OrbitSpeed, delta.Y * OrbitSpeed);
            _autoRotate = false;
        }

        if (hovered)
        {
            float wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0f) _camera.Zoom(wheel);
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) ResetView();
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) _autoRotate = !_autoRotate;
        }

        var drawList = ImGui.GetWindowDrawList();
        string hint = _content == null ? "No preview" : _content.IsReady ? "Drag: rotate  Wheel: zoom  Double click: reset  RMB: auto rotate" : "Generating...";
        drawList.AddText(start + new Vector2(6f, 4f), 0xB0FFFFFF, hint);
    }

    public void Update(float deltaTime)
    {
        if (_content == null) return;

        _content.Update(deltaTime);
        if (_content.IsReady && !_framed) Reframe();
        if (_autoRotate) _camera.Spin(OpenTK.Mathematics.Vector3.UnitY, AutoRotateSpeed * deltaTime);
    }

    public void Render()
    {
        if (!_visible) return;
        _visible = false;

        var content = _content;
        var background = Background;
        _target.Render(_camera, () =>
        {
            background?.Invoke();
            if (content != null && content.IsReady) content.Render();
        });
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
