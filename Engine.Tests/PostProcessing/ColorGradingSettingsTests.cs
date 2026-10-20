using Engine.PostProcessing.ColorGrading;

namespace Engine.Tests.PostProcessing;

public class ColorGradingSettingsTests
{
    [Fact]
    public void Default_IsNeutral()
    {
        Assert.True(ColorGradingSettings.Default.IsNeutral);
    }

    [Theory]
    [InlineData(1.2f, 1f, 1f, 0f)]
    [InlineData(1f, 0.8f, 1f, 0f)]
    [InlineData(1f, 1f, 0f, 0f)]
    [InlineData(1f, 1f, 1f, 0.1f)]
    public void AnyChange_IsNotNeutral(float brightness, float contrast, float saturation, float grain)
    {
        var settings = new ColorGradingSettings
        {
            Brightness = brightness,
            Contrast = contrast,
            Saturation = saturation,
            FilmGrain = grain
        };

        Assert.False(settings.IsNeutral);
    }

    [Fact]
    public void Clamped_KeepsValuesInRange()
    {
        var settings = new ColorGradingSettings
        {
            Brightness = 9f,
            Contrast = 0f,
            Saturation = -1f,
            FilmGrain = 5f
        }.Clamped();

        Assert.Equal(ColorGradingSettings.MaxBrightness, settings.Brightness);
        Assert.Equal(ColorGradingSettings.MinContrast, settings.Contrast);
        Assert.Equal(0f, settings.Saturation);
        Assert.Equal(1f, settings.FilmGrain);
    }
}
