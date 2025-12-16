using Engine;
using Engine.Audio;
using ImGuiNET;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;
using System.Numerics;

namespace Spacebox.Game.GUI;

public class RadarUI : IDisposable
{
    public static RadarUI Instance;

    private bool _isVisible = false;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            if (_isVisible)
            {
                _openAudio?.Play();
            }
            else
            {
                _closeAudio?.Play();
            }
        }
    }

    public event Action OnOpen;
    public event Action OnClose;

    private Texture2D _spaceBackground;
    private Texture2D _gridTexture;
    private Texture2D _lineTexture;
    private Texture2D _scanningTexture;
    private Texture2D _maskTexture;

    private AudioSource _scanningAudio;
    private AudioSource _foundAudio;
    private AudioSource _openAudio;
    private AudioSource _closeAudio;

    private Vector2 _bgUvPos = Vector2.Zero;
    private Vector2 _bgUvMax = Vector2.Zero;
    private Vector2 _bgUvSize = new Vector2(0.15f, 0.15f);
    private Vector2 _bgMoveDir = new Vector2(0.7f, 0.5f);

    private float _radarRotationAngle = 0f;
    private float _rotationSpeed = MathF.PI / 2 * 0.8f;
    private float _scanWaveProgress = 0;

    private bool _isScanWaveActive = false;
    private bool _showSpaceBackground = false;
    private bool _isTargetPointVisible = false;

    private float _pointVisibilityTimer = 2;

    public RadarUI(Texture2D skyboxTexture)
    {
        _spaceBackground = skyboxTexture;
        Instance = this;
        _bgUvMax = _bgUvSize;

        _gridTexture = Resources.Load<Texture2D>("Resources/Textures/UI/Radar/radarGrid.png");
        _lineTexture = Resources.Load<Texture2D>("Resources/Textures/UI/Radar/radarLine.png");
        _scanningTexture = Resources.Load<Texture2D>("Resources/Textures/UI/Radar/scanning.png");
        _maskTexture = Resources.Load<Texture2D>("Resources/Textures/UI/Radar/radarMask.png");

        _gridTexture.FilterMode = FilterMode.Nearest;
        _lineTexture.FilterMode = FilterMode.Nearest;
        _scanningTexture.FilterMode = FilterMode.Nearest;
        _maskTexture.FilterMode = FilterMode.Nearest;

        _scanningAudio = new AudioSource(Resources.Load<AudioClip>("radarScanning"));
        _foundAudio = new AudioSource(Resources.Load<AudioClip>("radarFound"));

        var inventory = ToggleManager.Register("radar");
        inventory.IsUI = true;
        inventory.OnStateChanged += s =>
        {
            IsVisible = s;
        };

        if (_openAudio == null)
        {
            _openAudio = new AudioSource(Resources.Load<AudioClip>("openBlock1"));
            _openAudio.Volume = 1f;
        }

        if (_closeAudio == null)
        {
            _closeAudio = new AudioSource(Resources.Load<AudioClip>("openBlock4"));
            _closeAudio.Volume = 1f;
        }
    }

    public void OnGUI()
    {
        if (!IsVisible) return;

        Vector2 displaySize = ImGui.GetIO().DisplaySize;
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));

        float windowWidth = displaySize.Y * 0.4f;
        float windowHeight = displaySize.Y * 0.4f;
        var windowPos = GameMenu.CenterNextWindow(windowWidth, windowHeight);

        ImGui.Begin("Radar", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoScrollbar);

        var drawList = ImGui.GetWindowDrawList();
        float padding = windowHeight * 0.03f;
        Vector2 contentMin = windowPos + new Vector2(padding, padding);
        Vector2 contentMax = windowPos + new Vector2(windowWidth, windowHeight) - new Vector2(padding, padding);
        Vector2 center = new Vector2(windowWidth / 2, windowHeight / 2) + windowPos;

        GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);

        UpdateBackgroundUV();
        DrawBackground(drawList, contentMin, contentMax);
        UpdateRadarLogic();
        UpdateRadarAudio();


        var direction = GetDirectionFromDegrees(255); // 0 = right, 90 = up, 180 = left, 270 = down
        float distance = windowHeight * 0.5f * MapDistanceToRadarRadius(500, 1000); // 0.1 min  0.85 max
        Vector2 pointPos = GetPointPosition(center, direction, distance);

        DrawTargetPoint(drawList, center, pointPos, 8);
        DrawScanningEffect(drawList, center, new Vector2(windowWidth, windowHeight));
        DrawRotatingLine(drawList, center, new Vector2(windowWidth, windowHeight));
        DrawMask(drawList, contentMin, contentMax);

        ImGui.PopStyleColor(1);
        ImGui.End();
    }

    private void DrawBackground(ImDrawListPtr drawList, Vector2 min, Vector2 max)
    {
        drawList.AddImage(_spaceBackground.Handle, min, max, _bgUvPos, _bgUvMax);
        var color = new Color3Byte(62, 150, 67).ToVector3();
        uint tintColor = ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, _showSpaceBackground ? 0 : 1));
        drawList.AddRectFilled(min, max, tintColor);

        drawList.AddImage(_gridTexture.Handle, min, max);
    }

    private void UpdateRadarLogic()
    {
        _radarRotationAngle += _rotationSpeed * Time.Delta;
        _radarRotationAngle %= MathF.PI * 2;
    }

    private void UpdateRadarAudio()
    {
        if (_radarRotationAngle >= 3.1f && _radarRotationAngle < 3.2f)
        {
            if (!_scanningAudio.IsPlaying)
            {
                _scanningAudio.Play();
                _scanWaveProgress = 0;
                _isScanWaveActive = true;
            }
        }

        if (_radarRotationAngle >= 4.99f && _radarRotationAngle < 5.1f)
        {
            if (!_foundAudio.IsPlaying)
            {
                _foundAudio.Play();
                _isTargetPointVisible = true;
            }
        }
    }

    private void DrawTargetPoint(ImDrawListPtr drawList, Vector2 centerOfWindow, Vector2 positionInWindow, int radiusBase)
    {
        if (!_isTargetPointVisible) return;

        _pointVisibilityTimer -= Time.Delta;

        if (_pointVisibilityTimer <= 0)
        {
            _pointVisibilityTimer = 0;
        }

        float pointRadius = centerOfWindow.Y * radiusBase * 0.0015f;
        float alpha = Math.Clamp(_pointVisibilityTimer / 2f, 0f, 1f);

        drawList.AddRectFilled(
            positionInWindow - new Vector2(pointRadius, pointRadius),
            positionInWindow + new Vector2(pointRadius, pointRadius),
            ImGui.GetColorU32(new Vector4(1, 1, 1, alpha)));

        if (_pointVisibilityTimer <= 0)
        {
            _pointVisibilityTimer = 2;
            _isTargetPointVisible = false;
        }
    }

    private void DrawScanningEffect(ImDrawListPtr drawList, Vector2 center, Vector2 size)
    {
        if (!_isScanWaveActive) return;

        _scanWaveProgress += 2f * Time.Delta;
        if (_scanWaveProgress > 0.99f)
        {
            _isScanWaveActive = false;
            _scanWaveProgress = 0;
        }

        float alpha = Math.Clamp(1.0f - _scanWaveProgress, 0f, 1f);
        Vector2 halfSize = size * 0.5f;

        drawList.AddImage(_scanningTexture.Handle,
            center - halfSize * _scanWaveProgress,
            center + halfSize * _scanWaveProgress,
            Vector2.Zero, Vector2.One,
            ImGui.GetColorU32(new Vector4(1, 1, 1, alpha)));
    }

    private void DrawRotatingLine(ImDrawListPtr drawList, Vector2 center, Vector2 size)
    {
        Vector2 halfSize = size * 0.5f;
        Vector2[] corners = new Vector2[]
        {
            new Vector2(-halfSize.X, -halfSize.Y),
            new Vector2(-halfSize.X, halfSize.Y),
            new Vector2(halfSize.X, halfSize.Y),
            new Vector2(halfSize.X, -halfSize.Y)
        };

        float cos = MathF.Cos(_radarRotationAngle);
        float sin = MathF.Sin(_radarRotationAngle);

        for (int i = 0; i < corners.Length; i++)
        {
            float x = corners[i].X * cos - corners[i].Y * sin;
            float y = corners[i].X * sin + corners[i].Y * cos;
            corners[i] = new Vector2(x, y) + center;
        }

        drawList.AddImageQuad(
            _lineTexture.Handle,
            corners[0], corners[1], corners[2], corners[3],
            Vector2.Zero, new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0),
            ImGui.GetColorU32(new Vector4(1, 1, 1, 1))
        );
    }

    private void DrawMask(ImDrawListPtr drawList, Vector2 min, Vector2 max)
    {
        drawList.AddImage(_maskTexture.Handle, min, max);
    }

    // interpolation from 0.1 to 0.85
    public float MapDistanceToRadarRadius(float currentDistance, float maxDistance)
    {
        if (maxDistance <= 0) return 0.1f;
        float t = Math.Clamp(currentDistance / maxDistance, 0f, 1f);
        return 0.1f + (0.85f - 0.1f) * t;
    }

    public Vector2 GetPointPosition(Vector2 windowCenter, Vector2 direction, float distance)
    {
        return windowCenter + direction * distance;
    }

    private Vector2 GetDirectionFromRadians(float radians)
    {
        float x = MathF.Cos(radians);
        float y = -MathF.Sin(radians);
        return new Vector2(x, y);
    }

    // 0 = right, 90 = up, 180 = left, 270 = down
    private Vector2 GetDirectionFromDegrees(float degrees)
    {
        float radians = (MathF.PI / 180f) * degrees;
        return GetDirectionFromRadians(radians);
    }

    private void UpdateBackgroundUV()
    {
        _bgUvPos.X = CalculateBounceAxis(_bgUvPos.X, _bgUvSize.X, ref _bgMoveDir.X);
        _bgUvPos.Y = CalculateBounceAxis(_bgUvPos.Y, _bgUvSize.Y, ref _bgMoveDir.Y);
        _bgUvMax = _bgUvPos + _bgUvSize;
    }

    private static float CalculateBounceAxis(float current, float size, ref float dir)
    {
        float next = current + dir * Time.Delta * 0.005f;

        if (next < 0)
        {
            dir *= -1;
            return 0;
        }

        if (next + size > 1)
        {
            dir *= -1;
            return 1 - size;
        }

        return next;
    }

    public static void CenterButtonWithBackground(string label, float width, float height, Action onClick)
    {
        float windowWidth = ImGui.GetWindowWidth();
        float cursorX = (windowWidth - width) * 0.5f;
        ImGui.SetCursorPosX(cursorX);

        Vector2 buttonPos = ImGui.GetCursorScreenPos();
        float offsetValue = height * 0.1f;
        Vector2 offset = new Vector2(offsetValue, offsetValue);

        uint borderColor = ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1f));
        uint lightColor = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f));

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(buttonPos - offset, buttonPos + new Vector2(width, height) + offset, borderColor);
        drawList.AddRectFilled(buttonPos, buttonPos + new Vector2(width, height), lightColor);

        if (ImGui.Button(label, new Vector2(width, height)))
        {
            onClick?.Invoke();
        }
    }

    public void Toggle(Astronaut _)
    {
        var v = !IsVisible;
        ToggleManager.DisableAllWindows();

        if (v)
        {
            OnOpen?.Invoke();
            ToggleManager.SetState("mouse", true);
            ToggleManager.SetState("player", false);
            ToggleManager.SetState("radar", v);
            ToggleManager.SetState("panel", false);
        }
        else
        {
            OnClose?.Invoke();
            ToggleManager.SetState("mouse", false);
            ToggleManager.SetState("player", true);
            ToggleManager.SetState("panel", true);
        }
    }

    public void Dispose()
    {
        _foundAudio?.Dispose();
        _scanningAudio?.Dispose();
        _openAudio?.Dispose();
        _closeAudio?.Dispose();

        _spaceBackground?.Dispose();
        _gridTexture?.Dispose();
        _lineTexture?.Dispose();
        _scanningTexture?.Dispose();
        _maskTexture?.Dispose();

        Instance = null;
    }
}