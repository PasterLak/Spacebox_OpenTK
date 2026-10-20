using System.Text.Json;
using Engine.PostProcessing.Ssao;
using Spacebox.Game;
using Spacebox.Game.Rendering;

namespace Spacebox.Tests.Game;

public class SsaoSettingsMapperTests
{
    private static readonly GraphicsSettings Defaults = new GraphicsSettings();

    private static void AssertSsaoDefaults(GraphicsSettings graphics)
    {
        Assert.Equal(Defaults.SsaoQuality, graphics.SsaoQuality);
        Assert.Equal(Defaults.SsaoStrengthPercent, graphics.SsaoStrengthPercent);
        Assert.Equal(Defaults.SsaoSmoothnessPercent, graphics.SsaoSmoothnessPercent);
        Assert.Equal(Defaults.SsaoGrainPercent, graphics.SsaoGrainPercent);
        Assert.Equal(Defaults.SsaoGrainSize, graphics.SsaoGrainSize);
        Assert.Equal(Defaults.SsaoRadiusPercent, graphics.SsaoRadiusPercent);
    }

    private static GameSettings Parse(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var settings = JsonSerializer.Deserialize<GameSettings>(json, options)!;
        settings.ValidateAgainst(GameSettings.Default());
        return settings;
    }

    [Fact]
    public void OldSettings_WithoutSsaoKeys_UseDefaults()
    {
        var settings = Parse("{ \"graphics\": { \"fov\": 70 } }");

        AssertSsaoDefaults(settings.Graphics);
    }

    [Fact]
    public void GrainAndRadius_OutOfRange_UseDefaults()
    {
        var settings = Parse("{ \"graphics\": { \"ssao_grain_percent\": 150, \"ssao_radius_percent\": 5, \"ssao_grain_size\": 0 } }");

        Assert.Equal(Defaults.SsaoGrainSize, settings.Graphics.SsaoGrainSize);
        Assert.Equal(Defaults.SsaoGrainPercent, settings.Graphics.SsaoGrainPercent);
        Assert.Equal(Defaults.SsaoRadiusPercent, settings.Graphics.SsaoRadiusPercent);
    }

    [Fact]
    public void From_MapsGrainAndRadius()
    {
        var graphics = new GraphicsSettings { SsaoGrainPercent = 60, SsaoRadiusPercent = 150, SsaoGrainSize = 4 };
        var settings = SsaoSettingsMapper.From(graphics);

        Assert.Equal(4, settings.GrainSize);

        Assert.Equal(0.6f, settings.Grain, 3);
        Assert.Equal(1.5f, settings.Radius, 3);
    }

    [Fact]
    public void Smoothness_OutOfRange_UsesDefault()
    {
        var settings = Parse("{ \"graphics\": { \"ssao_smoothness_percent\": 300 } }");

        Assert.Equal(Defaults.SsaoSmoothnessPercent, settings.Graphics.SsaoSmoothnessPercent);
    }

    [Fact]
    public void From_MapsSmoothnessPercent()
    {
        var graphics = new GraphicsSettings { SsaoSmoothnessPercent = 25 };

        Assert.Equal(0.25f, SsaoSettingsMapper.From(graphics).Smoothness, 3);
    }

    [Theory]
    [InlineData(-1, 500)]
    [InlineData(9, 201)]
    public void OutOfRangeValues_AreReplacedByDefaults(int quality, int strength)
    {
        var settings = Parse($"{{ \"graphics\": {{ \"ssao_quality\": {quality}, \"ssao_strength_percent\": {strength} }} }}");

        Assert.Equal(Defaults.SsaoQuality, settings.Graphics.SsaoQuality);
        Assert.Equal(Defaults.SsaoStrengthPercent, settings.Graphics.SsaoStrengthPercent);
    }

    [Fact]
    public void Defaults_AreTheTunedLook()
    {
        Assert.Equal(2, Defaults.SsaoQuality);
        Assert.Equal(99, Defaults.SsaoStrengthPercent);
        Assert.Equal(38, Defaults.SsaoSmoothnessPercent);
        Assert.Equal(29, Defaults.SsaoGrainPercent);
        Assert.Equal(2, Defaults.SsaoGrainSize);
        Assert.Equal(31, Defaults.SsaoRadiusPercent);
    }

    [Fact]
    public void ResetSsao_RestoresDefaultsAndKeepsOtherSettings()
    {
        var graphics = new GraphicsSettings
        {
            Fov = 70,
            SsaoQuality = 3,
            SsaoStrengthPercent = 150,
            SsaoSmoothnessPercent = 0,
            SsaoGrainPercent = 90,
            SsaoGrainSize = 7,
            SsaoRadiusPercent = 250
        };

        graphics.ResetSsao();

        AssertSsaoDefaults(graphics);
        Assert.Equal(70, graphics.Fov);
    }

    [Theory]
    [InlineData(0, SsaoQuality.Off)]
    [InlineData(1, SsaoQuality.Low)]
    [InlineData(2, SsaoQuality.Medium)]
    [InlineData(3, SsaoQuality.High)]
    public void From_MapsQuality(int value, SsaoQuality expected)
    {
        var graphics = new GraphicsSettings { SsaoQuality = value };

        Assert.Equal(expected, SsaoSettingsMapper.From(graphics).Quality);
    }

    [Fact]
    public void From_MapsStrengthPercent()
    {
        var graphics = new GraphicsSettings { SsaoStrengthPercent = 150 };

        Assert.Equal(1.5f, SsaoSettingsMapper.From(graphics).Strength, 3);
    }

    [Fact]
    public void From_OffDisablesEffect()
    {
        var graphics = new GraphicsSettings { SsaoQuality = 0 };

        Assert.False(SsaoSettingsMapper.From(graphics).IsEnabled);
    }
}
