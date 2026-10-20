using System.Text.Json;
using Spacebox.Game;
using Spacebox.Game.Rendering;

namespace Spacebox.Tests.Game;

public class ColorGradingSettingsMapperTests
{
    private static readonly GraphicsSettings Defaults = new GraphicsSettings();

    private static GameSettings Parse(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var settings = JsonSerializer.Deserialize<GameSettings>(json, options)!;
        settings.ValidateAgainst(GameSettings.Default());
        return settings;
    }

    private static void AssertImageDefaults(GraphicsSettings graphics)
    {
        Assert.Equal(Defaults.BrightnessPercent, graphics.BrightnessPercent);
        Assert.Equal(Defaults.ContrastPercent, graphics.ContrastPercent);
        Assert.Equal(Defaults.SaturationPercent, graphics.SaturationPercent);
        Assert.Equal(Defaults.VignettePercent, graphics.VignettePercent);
        Assert.Equal(Defaults.FilmGrainPercent, graphics.FilmGrainPercent);
        Assert.Equal(Defaults.FilmGrainAnimated, graphics.FilmGrainAnimated);
    }

    [Fact]
    public void OldSettings_WithoutImageKeys_UseDefaults()
    {
        var settings = Parse("{ \"graphics\": { \"fov\": 70 } }");

        AssertImageDefaults(settings.Graphics);
        Assert.False(settings.Graphics.ShowFps);
    }

    [Fact]
    public void Defaults_KeepTheImageUnchanged()
    {
        Assert.True(ColorGradingSettingsMapper.From(Defaults).IsNeutral);
        Assert.Equal(50, Defaults.VignettePercent);
    }

    [Fact]
    public void OutOfRangeValues_AreReplacedByDefaults()
    {
        var settings = Parse("{ \"graphics\": { \"brightness_percent\": 10, \"contrast_percent\": 400, \"saturation_percent\": -5, \"vignette_percent\": 101, \"film_grain_percent\": 300 } }");

        AssertImageDefaults(settings.Graphics);
    }

    [Fact]
    public void From_MapsPercentsToFactors()
    {
        var graphics = new GraphicsSettings
        {
            BrightnessPercent = 120,
            ContrastPercent = 80,
            SaturationPercent = 150,
            FilmGrainPercent = 40,
            FilmGrainAnimated = false
        };

        var settings = ColorGradingSettingsMapper.From(graphics);

        Assert.Equal(1.2f, settings.Brightness, 3);
        Assert.Equal(0.8f, settings.Contrast, 3);
        Assert.Equal(1.5f, settings.Saturation, 3);
        Assert.Equal(0.4f, settings.FilmGrain, 3);
        Assert.False(settings.FilmGrainAnimated);
    }

    [Fact]
    public void ResetImage_RestoresImageKeysOnly()
    {
        var graphics = new GraphicsSettings
        {
            BrightnessPercent = 140,
            ContrastPercent = 60,
            SaturationPercent = 0,
            VignettePercent = 0,
            FilmGrainPercent = 70,
            FilmGrainAnimated = false,
            SsaoGrainPercent = 80,
            ShowFps = true
        };

        graphics.ResetImage();

        AssertImageDefaults(graphics);
        Assert.Equal(80, graphics.SsaoGrainPercent);
        Assert.True(graphics.ShowFps);
    }
}
