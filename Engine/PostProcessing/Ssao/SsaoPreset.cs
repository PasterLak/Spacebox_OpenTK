namespace Engine.PostProcessing.Ssao;

public readonly struct SsaoPreset
{
    public const int MaxSamples = 32;

    public int ResolutionDivisor { get; }
    public int SampleCount { get; }
    public int BlurRadius { get; }

    public SsaoPreset(int resolutionDivisor, int sampleCount, int blurRadius)
    {
        ResolutionDivisor = Math.Max(1, resolutionDivisor);
        SampleCount = Math.Clamp(sampleCount, 1, MaxSamples);
        BlurRadius = Math.Max(0, blurRadius);
    }

    public int BlurRadiusFor(float smoothness)
    {
        return (int)MathF.Round(BlurRadius * 2f * Math.Clamp(smoothness, 0f, 1f));
    }

    public static SsaoPreset For(SsaoQuality quality)
    {
        return quality switch
        {
            SsaoQuality.Low => new SsaoPreset(2, 8, 2),
            SsaoQuality.Medium => new SsaoPreset(2, 16, 3),
            SsaoQuality.High => new SsaoPreset(1, 16, 4),
            _ => new SsaoPreset(2, 8, 2)
        };
    }
}
