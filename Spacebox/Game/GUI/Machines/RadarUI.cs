using Engine;
using Engine.Audio;
using Spacebox.Game.Player;
using Spacebox.Game.Generation.Blocks;
using OpenTK.Mathematics;
using System;

namespace Spacebox.Game.GUI;

public sealed class RadarUI : IGameUiController, IDisposable
{
    private bool _wasVisible = false;

    public event Action OnOpen;
    public event Action OnClose;

    private Texture2D _spaceBackground;
    private Texture2D _gridTexture;
    private Texture2D _lineTexture;
    private Texture2D _scanningTexture;
    private Texture2D _maskTexture;

    private AudioSource _scanningAudio;
    private AudioSource _foundAudio;

    private Vector2 _bgUvPos = Vector2.Zero;
    private Vector2 _bgUvMax = Vector2.Zero;
    private Vector2 _bgUvSize = new Vector2(0.15f, 0.15f);
    private Vector2 _bgMoveDir = new Vector2(0.7f, 0.5f);

    private float _radarRotationAngle = 0f;
    private float _rotationSpeed = MathF.PI / 2 * 0.8f;
    private float _scanWaveProgress = 0;

    private bool _isScanWaveActive = false;
    private bool _showSpaceBackground = false;

    private readonly Spacebox.Game.Radar.RadarScanner _scanner = new Spacebox.Game.Radar.RadarScanner();
    private Astronaut _viewer;

    public RadarUI(Texture2D skyboxTexture)
    {
        _spaceBackground = skyboxTexture;
        _bgUvMax = _bgUvSize;

        _gridTexture = GameAssets.LoadResource<Texture2D>("Resources/Textures/UI/Radar/radarGrid.png");
        _lineTexture = GameAssets.LoadResource<Texture2D>("Resources/Textures/UI/Radar/radarLine.png");
        _scanningTexture = GameAssets.LoadResource<Texture2D>("Resources/Textures/UI/Radar/scanning.png");
        _maskTexture = GameAssets.LoadResource<Texture2D>("Resources/Textures/UI/Radar/radarMask.png");

        Spacebox.Game.Radar.RadarTextures.ClipToCircle(_gridTexture, false);
        Spacebox.Game.Radar.RadarTextures.ClipToCircle(_scanningTexture, false);
        Spacebox.Game.Radar.RadarTextures.ClipToCircle(_maskTexture, true);

        _gridTexture.FilterMode = FilterMode.Nearest;
        _lineTexture.FilterMode = FilterMode.Nearest;
        _scanningTexture.FilterMode = FilterMode.Nearest;
        _maskTexture.FilterMode = FilterMode.Nearest;

        _scanningAudio = new AudioSource(GameAssets.LoadResource<AudioClip>("radarScanning"), AudioChannel.UI);
        _foundAudio = new AudioSource(GameAssets.LoadResource<AudioClip>("radarFound"), AudioChannel.UI);
    }

    public RadarBlock Block { get; private set; }
    public float Range => Spacebox.Game.Radar.RadarScanner.Range;
    public IReadOnlyList<Spacebox.Game.Radar.RadarContact> Blips => _scanner.Blips;

    public bool IsOpen => UIManager.IsOpen("radar");
    public Texture2D SpaceBackground => _spaceBackground;
    public Texture2D GridTexture => _gridTexture;
    public Texture2D LineTexture => _lineTexture;
    public Texture2D ScanningTexture => _scanningTexture;
    public Texture2D MaskTexture => _maskTexture;
    public Vector2 BackgroundUvPosition => _bgUvPos;
    public Vector2 BackgroundUvSize => _bgUvSize;
    public bool ShowsSpaceBackground => _showSpaceBackground;
    public float Angle => _radarRotationAngle;
    public bool IsScanWaveActive => _isScanWaveActive;
    public float ScanWaveProgress => _scanWaveProgress;

    public void OnGUI()
    {
        bool isVisible = IsOpen;

        if (isVisible != _wasVisible)
        {
            if (isVisible)
            {
                OnOpen?.Invoke();
            }
            else
            {
                OnClose?.Invoke();
            }
            _wasVisible = isVisible;
        }

        if (!isVisible) return;

        UpdateBackgroundUV();
        float previousAngle = _radarRotationAngle;
        UpdateRadarLogic();
        UpdateRadarAudio();
        UpdateContacts(previousAngle);
        UpdateScanWave();
    }

    private void UpdateContacts(float previousAngle)
    {
        float fadePerSecond = _rotationSpeed / (MathF.PI * 2f);
        if (_scanner.Sweep(_viewer, previousAngle, _radarRotationAngle, fadePerSecond, Time.Delta) && !_foundAudio.IsPlaying)
            _foundAudio.Play();
    }

    private void UpdateScanWave()
    {
        if (!_isScanWaveActive) return;

        _scanWaveProgress += 2f * Time.Delta;
        if (_scanWaveProgress <= 0.99f) return;

        _isScanWaveActive = false;
        _scanWaveProgress = 0;
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

    public void Open(RadarBlock block, Astronaut viewer)
    {
        Block = block;
        _viewer = viewer;
        _scanner.Clear();
        UIManager.Open("radar");
    }

    public void Reset()
    {
        Block = null;
        _viewer = null;
        _scanner.Clear();
        Dispose();
    }

    public void Dispose()
    {
        _foundAudio?.Dispose();
        _scanningAudio?.Dispose();
    }
}