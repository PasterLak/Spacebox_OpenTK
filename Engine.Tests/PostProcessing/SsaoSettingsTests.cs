using Engine.PostProcessing.Ssao;
using OpenTK.Mathematics;

namespace Engine.Tests.PostProcessing;

public class SsaoSettingsTests
{
    [Fact]
    public void Presets_TradeResolutionAndSamplesForQuality()
    {
        var low = SsaoPreset.For(SsaoQuality.Low);
        var medium = SsaoPreset.For(SsaoQuality.Medium);
        var high = SsaoPreset.For(SsaoQuality.High);

        Assert.Equal(2, low.ResolutionDivisor);
        Assert.Equal(2, medium.ResolutionDivisor);
        Assert.Equal(1, high.ResolutionDivisor);
        Assert.True(low.SampleCount < medium.SampleCount);
        Assert.True(medium.BlurRadius < high.BlurRadius);
        Assert.All(new[] { low, medium, high }, p => Assert.InRange(p.SampleCount, 1, SsaoPreset.MaxSamples));
    }

    [Fact]
    public void Preset_ClampsInvalidValues()
    {
        var preset = new SsaoPreset(0, 500, -3);

        Assert.Equal(1, preset.ResolutionDivisor);
        Assert.Equal(SsaoPreset.MaxSamples, preset.SampleCount);
        Assert.Equal(0, preset.BlurRadius);
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(0.5f, 3)]
    [InlineData(1f, 6)]
    [InlineData(5f, 6)]
    public void BlurRadiusFor_ScalesPresetRadiusBySmoothness(float smoothness, int expected)
    {
        Assert.Equal(expected, SsaoPreset.For(SsaoQuality.Medium).BlurRadiusFor(smoothness));
    }

    [Theory]
    [InlineData(SsaoQuality.Off, 1f, false)]
    [InlineData(SsaoQuality.Medium, 0f, false)]
    [InlineData(SsaoQuality.Low, 0.5f, true)]
    public void IsEnabled_NeedsQualityAndStrength(SsaoQuality quality, float strength, bool expected)
    {
        var settings = SsaoSettings.Default with { Quality = quality, Strength = strength };

        Assert.Equal(expected, settings.IsEnabled);
    }

    [Fact]
    public void Clamped_FixesOutOfRangeValues()
    {
        var settings = new SsaoSettings
        {
            Quality = (SsaoQuality)42,
            Strength = 9f,
            Smoothness = -2f,
            Grain = 4f,
            GrainSize = 99,
            ViewModelStrength = 3f,
            Radius = -1f,
            Bias = -1f,
            FadeStart = 50f,
            FadeEnd = 10f
        }.Clamped();

        Assert.Equal(SsaoQuality.Medium, settings.Quality);
        Assert.Equal(SsaoSettings.MaxStrength, settings.Strength);
        Assert.Equal(0f, settings.Smoothness);
        Assert.Equal(1f, settings.Grain);
        Assert.Equal(SsaoSettings.MaxGrainSize, settings.GrainSize);
        Assert.Equal(1f, settings.ViewModelStrength);
        Assert.Equal(SsaoSettings.MinRadius, settings.Radius);
        Assert.Equal(0f, settings.Bias);
        Assert.True(settings.FadeEnd > settings.FadeStart);
    }

    [Theory]
    [InlineData(0.01f, 2200f, 0.5f)]
    [InlineData(0.01f, 2200f, 37f)]
    [InlineData(0.01f, 100f, 0.12f)]
    public void LinearDepth_RecoversViewDistanceFromWindowDepth(float near, float far, float distance)
    {
        var projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(80f), 16f / 9f, near, far);
        var clip = new Vector4(0f, 0f, -distance, 1f) * projection;
        float depthWindow = clip.Z / clip.W * 0.5f + 0.5f;

        float linear = SsaoProjection.LinearDepth(SsaoProjection.Pack(projection), depthWindow);

        Assert.InRange(linear, distance * 0.999f, distance * 1.001f);
    }
}
