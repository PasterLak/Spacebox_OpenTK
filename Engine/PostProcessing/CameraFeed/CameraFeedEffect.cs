using Engine.Graphics.Rhi;
using OpenTK.Mathematics;

namespace Engine.PostProcessing.CameraFeed;

public sealed class CameraFeedEffect : PostProcessEffect
{
    private const float GrainFrameRate = 24f;

    private readonly Shader _shader;
    private float _intensity = 1f;
    private bool _active;

    public override bool IsRequired => true;

    public float Intensity
    {
        get => _intensity;
        set
        {
            _intensity = Math.Clamp(value, 0f, 1f);
            UpdateEnabled();
        }
    }

    public bool Active
    {
        get => _active;
        set
        {
            _active = value;
            UpdateEnabled();
        }
    }

    public CameraFeedEffect(Shader shader)
    {
        _shader = shader ?? throw new ArgumentNullException(nameof(shader));
        UpdateEnabled();
    }

    public override void Apply(int inputTexture, int outputFbo, Vector2i clientSize)
    {
        _shader.Use();
        _shader.SetInt("scene", 0);
        _shader.SetFloat("uIntensity", _intensity);
        _shader.SetFloat("uTime", Time.Total);
        _shader.SetInt("uGrainSeed", (int)(Time.Total * GrainFrameRate));
        _shader.SetVector2("uResolution", new Vector2(clientSize.X, clientSize.Y));
        Gpu.Device.BindTexture(0, inputTexture);
    }

    private void UpdateEnabled()
    {
        Enabled = _active && _intensity > 0f;
    }
}
