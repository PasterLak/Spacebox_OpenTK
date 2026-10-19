using System.Text.Json;
using Spacebox.Game;

namespace Spacebox.Tests;

public class SettingsTests
{
    private static GameSettings Parse(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var settings = JsonSerializer.Deserialize<GameSettings>(json, options)!;
        settings.ValidateAgainst(GameSettings.Default());
        return settings;
    }

    [Fact]
    public void OldDrawDistanceInChunks_IsReplacedByDefault()
    {
        var settings = Parse("{ \"game\": { \"draw_distance\": 12 } }");

        Assert.Equal(GameplaySettings.MaxDrawDistance, settings.Game.DrawDistance);
    }

    [Fact]
    public void RemovedOptions_AreIgnored()
    {
        var settings = Parse("{ \"graphics\": { \"shadows\": true, \"fov\": 70 }, \"game\": { \"keep_inventory\": true } }");

        Assert.Equal(70, settings.Graphics.Fov);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(144, 144)]
    [InlineData(10, 120)]
    [InlineData(1000, 120)]
    public void MaxFps_AllowsUnlimitedAndRange(int value, int expected)
    {
        var settings = Parse($"{{ \"graphics\": {{ \"max_fps\": {value} }} }}");

        Assert.Equal(expected, settings.Graphics.MaxFps);
    }

    [Fact]
    public void MouseSensitivity_OutOfRange_UsesDefault()
    {
        var settings = Parse("{ \"game\": { \"mouse_sensitivity\": 5000 } }");

        Assert.Equal(100, settings.Game.MouseSensitivity);
    }

    [Fact]
    public void VisibleRadii_FollowDrawDistance()
    {
        var previous = Settings.Gameplay;
        try
        {
            Settings.Gameplay = new GameplaySettings { DrawDistance = 500 };

            Assert.Equal(500, Settings.ENTITY_VISIBLE_RADIUS);
            Assert.Equal(450, Settings.CHUNK_VISIBLE_RADIUS);
        }
        finally
        {
            Settings.Gameplay = previous;
        }
    }
}
