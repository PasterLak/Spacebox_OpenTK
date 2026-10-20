namespace Engine.PostProcessing.Ssao;

public sealed record SsaoSettings
{
    public const float MaxStrength = 2f;
    public const float MinRadius = 0.1f;
    public const float MaxRadius = 4f;
    public const int MaxGrainSize = 8;

    public static SsaoSettings Default { get; } = new SsaoSettings();

    public SsaoQuality Quality { get; init; } = SsaoQuality.Medium;
    public float Strength { get; init; } = 1f;
    public float Smoothness { get; init; } = 0.5f;
    public float Grain { get; init; }
    public int GrainSize { get; init; } = 2;
    public float ViewModelStrength { get; init; } = 0.4f;
    public float Radius { get; init; } = 0.75f;
    public float ViewModelRadius { get; init; } = 0.04f;
    public float Bias { get; init; } = 0.03f;
    public float FadeStart { get; init; } = 48f;
    public float FadeEnd { get; init; } = 96f;

    public bool IsEnabled => Quality != SsaoQuality.Off && Strength > 0f;

    public SsaoSettings Clamped()
    {
        float fadeStart = Math.Max(0f, FadeStart);
        return this with
        {
            Quality = Enum.IsDefined(Quality) ? Quality : SsaoQuality.Medium,
            Strength = Math.Clamp(Strength, 0f, MaxStrength),
            Smoothness = Math.Clamp(Smoothness, 0f, 1f),
            Grain = Math.Clamp(Grain, 0f, 1f),
            GrainSize = Math.Clamp(GrainSize, 1, MaxGrainSize),
            ViewModelStrength = Math.Clamp(ViewModelStrength, 0f, 1f),
            Radius = Math.Clamp(Radius, MinRadius, MaxRadius),
            ViewModelRadius = Math.Max(0.001f, ViewModelRadius),
            Bias = Math.Max(0f, Bias),
            FadeStart = fadeStart,
            FadeEnd = Math.Max(fadeStart + 0.01f, FadeEnd)
        };
    }
}
