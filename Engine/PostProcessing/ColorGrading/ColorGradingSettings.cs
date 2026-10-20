namespace Engine.PostProcessing.ColorGrading;

public sealed record ColorGradingSettings
{
    public const float MinBrightness = 0.5f;
    public const float MaxBrightness = 1.5f;
    public const float MinContrast = 0.5f;
    public const float MaxContrast = 1.5f;
    public const float MaxSaturation = 2f;

    public static ColorGradingSettings Default { get; } = new ColorGradingSettings();

    public float Brightness { get; init; } = 1f;
    public float Contrast { get; init; } = 1f;
    public float Saturation { get; init; } = 1f;
    public float FilmGrain { get; init; }
    public bool FilmGrainAnimated { get; init; } = true;

    public bool IsNeutral => Brightness == 1f && Contrast == 1f && Saturation == 1f && FilmGrain <= 0f;

    public ColorGradingSettings Clamped()
    {
        return this with
        {
            Brightness = Math.Clamp(Brightness, MinBrightness, MaxBrightness),
            Contrast = Math.Clamp(Contrast, MinContrast, MaxContrast),
            Saturation = Math.Clamp(Saturation, 0f, MaxSaturation),
            FilmGrain = Math.Clamp(FilmGrain, 0f, 1f)
        };
    }
}
