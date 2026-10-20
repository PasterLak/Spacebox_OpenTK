using Engine.PostProcessing.Ssao;

namespace Spacebox.Game.Rendering;

public static class SsaoSettingsMapper
{
    public static SsaoSettings From(GraphicsSettings graphics)
    {
        var quality = Enum.IsDefined(typeof(SsaoQuality), graphics.SsaoQuality)
            ? (SsaoQuality)graphics.SsaoQuality
            : SsaoSettings.Default.Quality;

        return SsaoSettings.Default with
        {
            Quality = quality,
            Strength = Math.Clamp(graphics.SsaoStrengthPercent, 0, GraphicsSettings.MaxSsaoStrengthPercent) / 100f,
            Smoothness = Math.Clamp(graphics.SsaoSmoothnessPercent, 0, 100) / 100f,
            Grain = Math.Clamp(graphics.SsaoGrainPercent, 0, 100) / 100f,
            GrainSize = Math.Clamp(graphics.SsaoGrainSize, 1, GraphicsSettings.MaxSsaoGrainSize),
            Radius = Math.Clamp(graphics.SsaoRadiusPercent, GraphicsSettings.MinSsaoRadiusPercent, GraphicsSettings.MaxSsaoRadiusPercent) / 100f
        };
    }
}
