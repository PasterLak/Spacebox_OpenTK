using ImGuiNET;
using System.Numerics;
using static Engine.EngineWindow;

namespace Spacebox.Game.GUI.Menu
{
    public class GraphicsWindow : MenuWindow
    {
        private readonly IOptionsNavigator _options;
        private readonly string[] _modes = { "Fullscreen", "Borderless", "Windowed" };
        private readonly string[] _ssaoQualities = { "Off", "Low", "Medium", "High" };
        private int _pendingResolution = -1;

        public GraphicsWindow(IOptionsNavigator options)
        {
            _options = options;
        }

        public override void Render()
        {
            var graphics = Settings.Graphics;

            SettingsUI.Render("Graphics", "Graphics", 5,
                (listSize, rowH) =>
                {
                    var rows = OptionsRows.Begin("table##graphics", listSize.X);

                    rows.Section("Display");
                    int mode = (int)graphics.WindowMode;
                    if (rows.Combo("Window Mode", "Fullscreen uses the whole monitor, Borderless is a window without frame covering the screen, Windowed is a normal window.", ref mode, _modes))
                        Apply(graphics, () => graphics.WindowMode = (WindowMode)mode);

                    bool vsync = graphics.VSync;
                    if (rows.Checkbox("VSync", "Matches the frame rate to the monitor refresh rate to avoid screen tearing. While on, Max FPS is not used.", ref vsync))
                        Apply(graphics, () => graphics.VSync = vsync);

                    int fps = graphics.MaxFps <= 0 ? GraphicsSettings.MaxFpsLimit + 1 : graphics.MaxFps;
                    if (graphics.VSync) ImGui.BeginDisabled();
                    if (rows.Slider("Max FPS", "Upper limit of frames per second when VSync is off. Lower limits save power and keep the computer cooler. The right end means unlimited.", ref fps, GraphicsSettings.MinFps, GraphicsSettings.MaxFpsLimit + 1, fps > GraphicsSettings.MaxFpsLimit ? "Unlimited" : "%d"))
                        Apply(graphics, () => graphics.MaxFps = fps > GraphicsSettings.MaxFpsLimit ? 0 : fps);
                    if (graphics.VSync) ImGui.EndDisabled();

                    bool showFps = graphics.ShowFps;
                    if (rows.Checkbox("Show FPS", "Shows frames per second and frame time in the top right corner.", ref showFps))
                        Apply(graphics, () => graphics.ShowFps = showFps);

                    int resolution = _pendingResolution >= 0 ? _pendingResolution : graphics.ResolutionScalePercent;
                    if (rows.Slider("Resolution Scale", "Renders the world at a lower resolution and stretches it to the window. The interface stays sharp. Lower values give more FPS.", ref resolution, 10, 100, "%d%%"))
                        _pendingResolution = resolution;
                    if (OptionsRows.LastItemReleased && _pendingResolution >= 0)
                    {
                        int value = _pendingResolution;
                        _pendingResolution = -1;
                        Apply(graphics, () => graphics.ResolutionScalePercent = value);
                    }

                    int fov = graphics.Fov;
                    if (rows.Slider("Field of View", "How wide the camera sees, in degrees. Higher values show more around you.", ref fov, 50, 120, "%d"))
                        Apply(graphics, () => graphics.Fov = fov);

                    rows.Section("Image");
                    int brightness = graphics.BrightnessPercent;
                    if (rows.Slider("Brightness", "Brightens or darkens the dark and middle tones of the image. Light sources stay bright.", ref brightness, GraphicsSettings.MinBrightnessPercent, GraphicsSettings.MaxBrightnessPercent, "%d%%"))
                        Apply(graphics, () => graphics.BrightnessPercent = brightness);

                    int contrast = graphics.ContrastPercent;
                    if (rows.Slider("Contrast", "Difference between dark and bright parts of the image.", ref contrast, GraphicsSettings.MinContrastPercent, GraphicsSettings.MaxContrastPercent, "%d%%"))
                        Apply(graphics, () => graphics.ContrastPercent = contrast);

                    int saturation = graphics.SaturationPercent;
                    if (rows.Slider("Saturation", "Color intensity. 0% is black and white, higher values give stronger colors.", ref saturation, 0, GraphicsSettings.MaxSaturationPercent, "%d%%"))
                        Apply(graphics, () => graphics.SaturationPercent = saturation);

                    int vignette = graphics.VignettePercent;
                    if (rows.Slider("Vignette", "Darkens the screen edges. Needs Post Processing. The low oxygen warning still darkens the edges when it is off.", ref vignette, 0, 100, "%d%%"))
                        Apply(graphics, () => graphics.VignettePercent = vignette);

                    int filmGrain = graphics.FilmGrainPercent;
                    if (rows.Slider("Film Grain", "Noise over the whole image for a retro film look.", ref filmGrain, 0, 100, "%d%%"))
                        Apply(graphics, () => graphics.FilmGrainPercent = filmGrain);

                    if (graphics.FilmGrainPercent > 0)
                    {
                        bool animated = graphics.FilmGrainAnimated;
                        if (rows.Checkbox("Animated Grain", "On: the grain changes 24 times per second like film. Off: the grain stays fixed on the screen.", ref animated))
                            Apply(graphics, () => graphics.FilmGrainAnimated = animated);
                    }

                    int cameraEffect = graphics.CameraEffectPercent;
                    if (rows.Slider("Camera Block Effect", "Security camera look while you view through a ship camera block: scanlines, grain, lens distortion and a REC overlay. 0% turns it off.", ref cameraEffect, 0, 100, "%d%%"))
                        Apply(graphics, () => graphics.CameraEffectPercent = cameraEffect);

                    bool flashlightGlareOn = graphics.FlashlightGlare;
                    if (rows.Checkbox("Flashlight Glare", "Glare, lit dirt and reflections on your helmet glass when another player within 5 m shines a flashlight in your face.", ref flashlightGlareOn))
                        Apply(graphics, () => graphics.FlashlightGlare = flashlightGlareOn);

                    if (graphics.FlashlightGlare)
                    {
                        int flashlightGlare = graphics.FlashlightGlarePercent;
                        if (rows.Slider("Glare Strength", "How strong the flashlight glare is.", ref flashlightGlare, 0, 100, "%d%%"))
                            Apply(graphics, () => graphics.FlashlightGlarePercent = flashlightGlare);
                    }

                    bool flashlightBeams = graphics.FlashlightBeams;
                    if (rows.Checkbox("Flashlight Beams", "Other players' flashlights show a light cone with dust floating in it.", ref flashlightBeams))
                        Apply(graphics, () => graphics.FlashlightBeams = flashlightBeams);

                    if (rows.Button("Image Defaults", "Restores the default brightness, contrast, saturation, vignette, film grain, camera block effect, flashlight glare and beams.", "Reset"))
                        Apply(graphics, graphics.ResetImage);

                    rows.Section("World");
                    bool ao = graphics.AO;
                    if (rows.Checkbox("Voxel Ambient Occlusion", "Darkens corners and edges where blocks meet, which makes shapes easier to read. Rebuilds the world meshes when changed.", ref ao))
                        Apply(graphics, () => graphics.AO = ao);

                    bool lighting = graphics.VoxelLighting;
                    if (rows.Checkbox("Voxel Lighting", "Light from lamps and glowing blocks spreads to the blocks around them. Turning it off is faster but leaves only the lamps themselves bright.", ref lighting))
                        Apply(graphics, () => graphics.VoxelLighting = lighting);

                    rows.Section("Effects");
                    bool post = graphics.PostProcessing;
                    if (rows.Checkbox("Post Processing", "Screen effects applied after the world is drawn, such as the vignette at the screen edges.", ref post))
                        Apply(graphics, () => graphics.PostProcessing = post);

                    int ssao = graphics.SsaoQuality;
                    if (rows.Combo("Ambient Occlusion (SSAO)", "Soft shadows in corners, under objects and on the item in your hand, computed on the screen. Low and Medium work at half resolution, High at full resolution and costs the most FPS.", ref ssao, _ssaoQualities))
                        Apply(graphics, () => graphics.SsaoQuality = ssao);

                    if (graphics.SsaoQuality > 0)
                    {
                        int strength = graphics.SsaoStrengthPercent;
                        if (rows.Slider("SSAO Strength", "How dark the screen space ambient occlusion shadows are. Does not change performance.", ref strength, 0, GraphicsSettings.MaxSsaoStrengthPercent, "%d%%"))
                            Apply(graphics, () => graphics.SsaoStrengthPercent = strength);

                        int smoothness = graphics.SsaoSmoothnessPercent;
                        if (rows.Slider("SSAO Smoothness", "How much the shadows are blurred. Low values keep fine detail but look grainy, high values give soft clean shadows.", ref smoothness, 0, 100, "%d%%"))
                            Apply(graphics, () => graphics.SsaoSmoothnessPercent = smoothness);

                        int grain = graphics.SsaoGrainPercent;
                        if (rows.Slider("SSAO Grain", "Turns the shadows into retro grain: dense dots near corners and edges that thin out further away. Combine with low Smoothness for the strongest effect. Does not change performance.", ref grain, 0, 100, "%d%%"))
                            Apply(graphics, () => graphics.SsaoGrainPercent = grain);

                        if (graphics.SsaoGrainPercent > 0)
                        {
                            int grainSize = graphics.SsaoGrainSize;
                            if (rows.Slider("SSAO Grain Size", "Size of one grain dot in screen pixels.", ref grainSize, 1, GraphicsSettings.MaxSsaoGrainSize, "%d px"))
                                Apply(graphics, () => graphics.SsaoGrainSize = grainSize);
                        }

                        int radius = graphics.SsaoRadiusPercent;
                        if (rows.Slider("SSAO Radius", "How far the shadows reach from corners and edges, in percent of a block. Large values darken whole walls near corners.", ref radius, GraphicsSettings.MinSsaoRadiusPercent, GraphicsSettings.MaxSsaoRadiusPercent, "%d%%"))
                            Apply(graphics, () => graphics.SsaoRadiusPercent = radius);
                    }

                    if (rows.Button("SSAO Defaults", "Restores the default SSAO quality, strength, smoothness, grain and radius.", "Reset"))
                        Apply(graphics, graphics.ResetSsao);

                    bool effects = graphics.EffectsEnabled;
                    if (rows.Checkbox("Particle Effects", "Space dust, mining sparks and similar particles. Turning them off gives a little more FPS.", ref effects))
                        Apply(graphics, () => graphics.EffectsEnabled = effects);

                    rows.End();
                },
                () =>
                {
                    _options.PlayClick();
                    SettingsService.Save(Settings.AsGameSettings());
                    _options.ShowOptions();
                },
                () =>
                {
                    _options.PlayClick();
                    _pendingResolution = -1;
                    OptionsMenu.DiscardUnsavedChanges();
                    _options.ShowOptions();
                }
            );
        }

        private static void Apply(GraphicsSettings graphics, Action change)
        {
            change();
            SettingsApplier.ApplyGraphics(graphics);
        }
    }
}
