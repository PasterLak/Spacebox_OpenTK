using Engine.PostProcessing.ColorGrading;

namespace Spacebox.Game.Rendering;

public static class ColorGradingSettingsMapper
{
    public static ColorGradingSettings From(GraphicsSettings graphics)
    {
        return new ColorGradingSettings
        {
            Brightness = graphics.BrightnessPercent / 100f,
            Contrast = graphics.ContrastPercent / 100f,
            Saturation = graphics.SaturationPercent / 100f,
            FilmGrain = graphics.FilmGrainPercent / 100f,
            FilmGrainAnimated = graphics.FilmGrainAnimated
        }.Clamped();
    }
}
