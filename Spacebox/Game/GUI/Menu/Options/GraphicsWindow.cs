using static Engine.EngineWindow;

namespace Spacebox.Game.GUI.Menu
{
    public class GraphicsWindow : RowsCategoryWindow
    {
        private readonly string[] _modes = { "Fullscreen", "Borderless", "Windowed" };
        private readonly string[] _ssaoQualities = { "Off", "Low", "Medium", "High" };

        public GraphicsWindow(IOptionsNavigator options) : base(options)
        {
        }

        public override string Title => "Graphics";

        private static GraphicsSettings G => Settings.Graphics;

        protected override IReadOnlyList<OptionRow> BuildRows()
        {
            var percent = SliderRow.Suffix("%");
            Func<bool> ssaoOn = () => G.SsaoQuality > 0;
            return new OptionRow[]
            {
                new SectionRow("Display"),
                new ChoiceRow("Window Mode", "Fullscreen uses the whole monitor, Borderless is a window without frame covering the screen, Windowed is a normal window.", _modes, () => (int)G.WindowMode, v => Apply(g => g.WindowMode = (WindowMode)v)),
                new CheckRow("VSync", "Matches the frame rate to the monitor refresh rate to avoid screen tearing. While on, Max FPS is not used.", () => G.VSync, v => Apply(g => g.VSync = v)),
                new SliderRow("Max FPS", "Upper limit of frames per second when VSync is off. Lower limits save power and keep the computer cooler. The right end means unlimited.", GraphicsSettings.MinFps, GraphicsSettings.MaxFpsLimit + 1,
                    () => G.MaxFps <= 0 ? GraphicsSettings.MaxFpsLimit + 1 : G.MaxFps,
                    v => Apply(g => g.MaxFps = v > GraphicsSettings.MaxFpsLimit ? 0 : v),
                    v => v > GraphicsSettings.MaxFpsLimit ? "Unlimited" : v.ToString()) { Default = () => Defaults.Graphics.MaxFps <= 0 ? GraphicsSettings.MaxFpsLimit + 1 : Defaults.Graphics.MaxFps, EnabledWhen = () => !G.VSync },
                new CheckRow("Show FPS", "Shows frames per second and frame time in the top right corner.", () => G.ShowFps, v => Apply(g => g.ShowFps = v)),
                new SliderRow("Resolution Scale", "Renders the world at a lower resolution and stretches it to the window. The interface stays sharp. Lower values give more FPS.", 10, 100, () => G.ResolutionScalePercent, v => Apply(g => g.ResolutionScalePercent = v), percent) { Default = () => Defaults.Graphics.ResolutionScalePercent, ApplyOnRelease = true },
                new SliderRow("Field of View", "How wide the camera sees, in degrees. Higher values show more around you.", 50, 120, () => G.Fov, v => Apply(g => g.Fov = v), v => v.ToString()) { Default = () => Defaults.Graphics.Fov },

                new SectionRow("Image"),
                new SliderRow("Brightness", "Brightens or darkens the dark and middle tones of the image. Light sources stay bright.", GraphicsSettings.MinBrightnessPercent, GraphicsSettings.MaxBrightnessPercent, () => G.BrightnessPercent, v => Apply(g => g.BrightnessPercent = v), percent) { Default = () => Defaults.Graphics.BrightnessPercent },
                new SliderRow("Contrast", "Difference between dark and bright parts of the image.", GraphicsSettings.MinContrastPercent, GraphicsSettings.MaxContrastPercent, () => G.ContrastPercent, v => Apply(g => g.ContrastPercent = v), percent) { Default = () => Defaults.Graphics.ContrastPercent },
                new SliderRow("Saturation", "Color intensity. 0% is black and white, higher values give stronger colors.", 0, GraphicsSettings.MaxSaturationPercent, () => G.SaturationPercent, v => Apply(g => g.SaturationPercent = v), percent) { Default = () => Defaults.Graphics.SaturationPercent },
                new SliderRow("Vignette", "Darkens the screen edges. Needs Post Processing. The low oxygen warning still darkens the edges when it is off.", 0, 100, () => G.VignettePercent, v => Apply(g => g.VignettePercent = v), percent) { Default = () => Defaults.Graphics.VignettePercent },
                new SliderRow("Film Grain", "Noise over the whole image for a retro film look.", 0, 100, () => G.FilmGrainPercent, v => Apply(g => g.FilmGrainPercent = v), percent) { Default = () => Defaults.Graphics.FilmGrainPercent },
                new CheckRow("Animated Grain", "On: the grain changes 24 times per second like film. Off: the grain stays fixed on the screen.", () => G.FilmGrainAnimated, v => Apply(g => g.FilmGrainAnimated = v)) { VisibleWhen = () => G.FilmGrainPercent > 0 },
                new SliderRow("Camera Block Effect", "Security camera look while you view through a ship camera block: scanlines, grain, lens distortion and a REC overlay. 0% turns it off.", 0, 100, () => G.CameraEffectPercent, v => Apply(g => g.CameraEffectPercent = v), percent) { Default = () => Defaults.Graphics.CameraEffectPercent },
                new CheckRow("Flashlight Glare", "Glare, lit dirt and reflections on your helmet glass when another player within 5 m shines a flashlight in your face.", () => G.FlashlightGlare, v => Apply(g => g.FlashlightGlare = v)),
                new SliderRow("Glare Strength", "How strong the flashlight glare is.", 0, 100, () => G.FlashlightGlarePercent, v => Apply(g => g.FlashlightGlarePercent = v), percent) { Default = () => Defaults.Graphics.FlashlightGlarePercent, VisibleWhen = () => G.FlashlightGlare },
                new CheckRow("Flashlight Beams", "Other players' flashlights show a light cone with dust floating in it.", () => G.FlashlightBeams, v => Apply(g => g.FlashlightBeams = v)),
                new ButtonRow("Image Defaults", "Restores the default brightness, contrast, saturation, vignette, film grain, camera block effect, flashlight glare and beams.", "Reset", () => Apply(g => g.ResetImage())),

                new SectionRow("World"),
                new CheckRow("Voxel Ambient Occlusion", "Darkens corners and edges where blocks meet, which makes shapes easier to read. Rebuilds the world meshes when changed.", () => G.AO, v => Apply(g => g.AO = v)),
                new CheckRow("Voxel Lighting", "Light from lamps and glowing blocks spreads to the blocks around them. Turning it off is faster but leaves only the lamps themselves bright.", () => G.VoxelLighting, v => Apply(g => g.VoxelLighting = v)),

                new SectionRow("Effects"),
                new CheckRow("Post Processing", "Screen effects applied after the world is drawn, such as the vignette at the screen edges.", () => G.PostProcessing, v => Apply(g => g.PostProcessing = v)),
                new ChoiceRow("Ambient Occlusion (SSAO)", "Soft shadows in corners, under objects and on the item in your hand, computed on the screen. Low and Medium work at half resolution, High at full resolution and costs the most FPS.", _ssaoQualities, () => G.SsaoQuality, v => Apply(g => g.SsaoQuality = v)),
                new SliderRow("SSAO Strength", "How dark the screen space ambient occlusion shadows are. Does not change performance.", 0, GraphicsSettings.MaxSsaoStrengthPercent, () => G.SsaoStrengthPercent, v => Apply(g => g.SsaoStrengthPercent = v), percent) { Default = () => Defaults.Graphics.SsaoStrengthPercent, VisibleWhen = ssaoOn },
                new SliderRow("SSAO Smoothness", "How much the shadows are blurred. Low values keep fine detail but look grainy, high values give soft clean shadows.", 0, 100, () => G.SsaoSmoothnessPercent, v => Apply(g => g.SsaoSmoothnessPercent = v), percent) { Default = () => Defaults.Graphics.SsaoSmoothnessPercent, VisibleWhen = ssaoOn },
                new SliderRow("SSAO Grain", "Turns the shadows into retro grain: dense dots near corners and edges that thin out further away. Combine with low Smoothness for the strongest effect. Does not change performance.", 0, 100, () => G.SsaoGrainPercent, v => Apply(g => g.SsaoGrainPercent = v), percent) { Default = () => Defaults.Graphics.SsaoGrainPercent, VisibleWhen = ssaoOn },
                new SliderRow("SSAO Grain Size", "Size of one grain dot in screen pixels.", 1, GraphicsSettings.MaxSsaoGrainSize, () => G.SsaoGrainSize, v => Apply(g => g.SsaoGrainSize = v), SliderRow.Suffix(" px")) { Default = () => Defaults.Graphics.SsaoGrainSize, VisibleWhen = () => G.SsaoQuality > 0 && G.SsaoGrainPercent > 0 },
                new SliderRow("SSAO Radius", "How far the shadows reach from corners and edges, in percent of a block. Large values darken whole walls near corners.", GraphicsSettings.MinSsaoRadiusPercent, GraphicsSettings.MaxSsaoRadiusPercent, () => G.SsaoRadiusPercent, v => Apply(g => g.SsaoRadiusPercent = v), percent) { Default = () => Defaults.Graphics.SsaoRadiusPercent, VisibleWhen = ssaoOn },
                new ButtonRow("SSAO Defaults", "Restores the default SSAO quality, strength, smoothness, grain and radius.", "Reset", () => Apply(g => g.ResetSsao())),
                new CheckRow("Particle Effects", "Space dust, mining sparks and similar particles. Turning them off gives a little more FPS.", () => G.EffectsEnabled, v => Apply(g => g.EffectsEnabled = v))
            };
        }

        private static void Apply(Action<GraphicsSettings> change)
        {
            var graphics = Settings.Graphics;
            change(graphics);
            SettingsApplier.ApplyGraphics(graphics);
        }
    }
}
