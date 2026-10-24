using Engine.Graphics.Rhi;
using OpenTK.Mathematics;

namespace Engine.PostProcessing.ColorGrading;

public sealed class ColorGradingEffect : PostProcessEffect
{
    private const float GrainFrameRate = 24f;

    private readonly Shader _shader;
    private ColorGradingSettings _settings = ColorGradingSettings.Default;

    public override bool IsRequired => true;
    public ColorGradingSettings Settings => _settings;

    public ColorGradingEffect(Shader shader)
    {
        _shader = shader ?? throw new ArgumentNullException(nameof(shader));
        Configure(ColorGradingSettings.Default);
    }

    public void Configure(ColorGradingSettings settings)
    {
        _settings = (settings ?? ColorGradingSettings.Default).Clamped();
        Enabled = !_settings.IsNeutral;
    }

    public override void Apply(int inputTexture, int outputFbo, Vector2i clientSize)
    {
        _shader.Use();
        _shader.SetInt("scene", 0);
        _shader.SetFloat("uGamma", _settings.Brightness);
        _shader.SetFloat("uContrast", _settings.Contrast);
        _shader.SetFloat("uSaturation", _settings.Saturation);
        _shader.SetFloat("uGrain", _settings.FilmGrain);
        _shader.SetInt("uGrainSeed", _settings.FilmGrainAnimated ? (int)(Time.Total * GrainFrameRate) : 0);
        Gpu.Device.BindTexture(0, inputTexture);
    }
}
