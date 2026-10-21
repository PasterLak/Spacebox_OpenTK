using Engine;
using Spacebox.Game.Events;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Engine.EngineWindow;


namespace Spacebox.Game
{
    

    public sealed class MetaSettings
    {
        [JsonPropertyName("schema_version")] public int SchemaVersion { get; set; } = 1;
    }

    public sealed class AudioSettings
    {
        [JsonPropertyName("master")] public int Master { get; set; } = 100;
        [JsonPropertyName("ambient")] public int Ambient { get; set; } = 80;
        [JsonPropertyName("music")] public int Music { get; set; } = 80;
        [JsonPropertyName("effects")] public int Effects { get; set; } = 90;
        [JsonPropertyName("ui")] public int UI { get; set; } = 75;
        [JsonPropertyName("mute_when_unfocused")] public bool MuteWhenUnfocused { get; set; } = false;
        [JsonPropertyName("menu_music")] public bool MenuMusic { get; set; } = true;

        public bool Validate(AudioSettings d)
        {
            bool changed = false;
            if (OutOfRange(Master, 0, 100)) { Master = d.Master; changed = true; }
            if (OutOfRange(Ambient, 0, 100)) { Ambient = d.Ambient; changed = true; }
            if (OutOfRange(Music, 0, 100)) { Music = d.Music; changed = true; }
            if (OutOfRange(Effects, 0, 100)) { Effects = d.Effects; changed = true; }
            if (OutOfRange(UI, 0, 100)) { UI = d.UI; changed = true; }
            return changed;
        }

        static bool OutOfRange(int v, int min, int max) => v < min || v > max;
    }

    public sealed class GraphicsSettings
    {
        [JsonPropertyName("window_mode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public WindowMode WindowMode { get; set; } = WindowMode.Windowed;

        [JsonPropertyName("vsync")] public bool VSync { get; set; } = true;
        [JsonPropertyName("ambient_occlusion")] public bool AO { get; set; } = true;
        [JsonPropertyName("voxel_lighting")] public bool VoxelLighting { get; set; } = true;
        [JsonPropertyName("post_processing")] public bool PostProcessing { get; set; } = true;
        [JsonPropertyName("effects_enabled")] public bool EffectsEnabled { get; set; } = true;
        [JsonPropertyName("fov")] public int Fov { get; set; } = 90;                           // 50..120
        [JsonPropertyName("resolution_scale_percent")] public int ResolutionScalePercent { get; set; } = 100; // 10..100
        [JsonPropertyName("max_fps")] public int MaxFps { get; set; } = 120;
        [JsonPropertyName("ssao_quality")] public int SsaoQuality { get; set; } = 2;
        [JsonPropertyName("ssao_strength_percent")] public int SsaoStrengthPercent { get; set; } = 99;
        [JsonPropertyName("ssao_smoothness_percent")] public int SsaoSmoothnessPercent { get; set; } = 38;
        [JsonPropertyName("ssao_grain_percent")] public int SsaoGrainPercent { get; set; } = 29;
        [JsonPropertyName("ssao_grain_size")] public int SsaoGrainSize { get; set; } = 2;
        [JsonPropertyName("ssao_radius_percent")] public int SsaoRadiusPercent { get; set; } = 31;
        [JsonPropertyName("brightness_percent")] public int BrightnessPercent { get; set; } = 100;
        [JsonPropertyName("contrast_percent")] public int ContrastPercent { get; set; } = 100;
        [JsonPropertyName("saturation_percent")] public int SaturationPercent { get; set; } = 100;
        [JsonPropertyName("vignette_percent")] public int VignettePercent { get; set; } = 50;
        [JsonPropertyName("film_grain_percent")] public int FilmGrainPercent { get; set; } = 0;
        [JsonPropertyName("film_grain_animated")] public bool FilmGrainAnimated { get; set; } = true;
        [JsonPropertyName("camera_effect_percent")] public int CameraEffectPercent { get; set; } = 100;
        [JsonPropertyName("flashlight_beams")] public bool FlashlightBeams { get; set; } = true;
        [JsonPropertyName("flashlight_glare")] public bool FlashlightGlare { get; set; } = true;
        [JsonPropertyName("flashlight_glare_percent")] public int FlashlightGlarePercent { get; set; } = 100;
        [JsonPropertyName("show_fps")] public bool ShowFps { get; set; } = false;

        public void ResetSsao()
        {
            var defaults = new GraphicsSettings();
            SsaoQuality = defaults.SsaoQuality;
            SsaoStrengthPercent = defaults.SsaoStrengthPercent;
            SsaoSmoothnessPercent = defaults.SsaoSmoothnessPercent;
            SsaoGrainPercent = defaults.SsaoGrainPercent;
            SsaoGrainSize = defaults.SsaoGrainSize;
            SsaoRadiusPercent = defaults.SsaoRadiusPercent;
        }

        public void ResetImage()
        {
            var defaults = new GraphicsSettings();
            BrightnessPercent = defaults.BrightnessPercent;
            ContrastPercent = defaults.ContrastPercent;
            SaturationPercent = defaults.SaturationPercent;
            VignettePercent = defaults.VignettePercent;
            FilmGrainPercent = defaults.FilmGrainPercent;
            FilmGrainAnimated = defaults.FilmGrainAnimated;
            CameraEffectPercent = defaults.CameraEffectPercent;
            FlashlightGlare = defaults.FlashlightGlare;
            FlashlightBeams = defaults.FlashlightBeams;
            FlashlightGlarePercent = defaults.FlashlightGlarePercent;
        }

        public bool Validate(GraphicsSettings d)
        {
            bool changed = false;
            if (OutOfRange(Fov, 50, 120)) { Fov = d.Fov; changed = true; }
            if (OutOfRange(ResolutionScalePercent, 10, 100)) { ResolutionScalePercent = d.ResolutionScalePercent; changed = true; }
            if (MaxFps != 0 && OutOfRange(MaxFps, MinFps, MaxFpsLimit)) { MaxFps = d.MaxFps; changed = true; }
            if (!Enum.IsDefined(WindowMode)) { WindowMode = d.WindowMode; changed = true; }
            if (OutOfRange(SsaoQuality, 0, MaxSsaoQuality)) { SsaoQuality = d.SsaoQuality; changed = true; }
            if (OutOfRange(SsaoStrengthPercent, 0, MaxSsaoStrengthPercent)) { SsaoStrengthPercent = d.SsaoStrengthPercent; changed = true; }
            if (OutOfRange(SsaoSmoothnessPercent, 0, 100)) { SsaoSmoothnessPercent = d.SsaoSmoothnessPercent; changed = true; }
            if (OutOfRange(SsaoGrainPercent, 0, 100)) { SsaoGrainPercent = d.SsaoGrainPercent; changed = true; }
            if (OutOfRange(SsaoGrainSize, 1, MaxSsaoGrainSize)) { SsaoGrainSize = d.SsaoGrainSize; changed = true; }
            if (OutOfRange(SsaoRadiusPercent, MinSsaoRadiusPercent, MaxSsaoRadiusPercent)) { SsaoRadiusPercent = d.SsaoRadiusPercent; changed = true; }
            if (OutOfRange(BrightnessPercent, MinBrightnessPercent, MaxBrightnessPercent)) { BrightnessPercent = d.BrightnessPercent; changed = true; }
            if (OutOfRange(ContrastPercent, MinContrastPercent, MaxContrastPercent)) { ContrastPercent = d.ContrastPercent; changed = true; }
            if (OutOfRange(SaturationPercent, 0, MaxSaturationPercent)) { SaturationPercent = d.SaturationPercent; changed = true; }
            if (OutOfRange(VignettePercent, 0, 100)) { VignettePercent = d.VignettePercent; changed = true; }
            if (OutOfRange(FilmGrainPercent, 0, 100)) { FilmGrainPercent = d.FilmGrainPercent; changed = true; }
            if (OutOfRange(CameraEffectPercent, 0, 100)) { CameraEffectPercent = d.CameraEffectPercent; changed = true; }
            if (OutOfRange(FlashlightGlarePercent, 0, 100)) { FlashlightGlarePercent = d.FlashlightGlarePercent; changed = true; }
            return changed;
        }

        public const int MinFps = 30;
        public const int MaxFpsLimit = 360;
        public const int MaxSsaoQuality = 3;
        public const int MaxSsaoStrengthPercent = 200;
        public const int MinSsaoRadiusPercent = 25;
        public const int MaxSsaoRadiusPercent = 300;
        public const int MaxSsaoGrainSize = 8;
        public const int MinBrightnessPercent = 50;
        public const int MaxBrightnessPercent = 150;
        public const int MinContrastPercent = 50;
        public const int MaxContrastPercent = 150;
        public const int MaxSaturationPercent = 200;

        static bool OutOfRange(int v, int min, int max) => v < min || v > max;
    }

    public sealed class InterfaceSettings
    {
        [JsonPropertyName("font")] public string Font { get; set; } = InterfaceFonts.DefaultId;
        [JsonPropertyName("font_size")] public int FontSize { get; set; } = InterfaceFonts.Find(InterfaceFonts.DefaultId).NativeSize;

        public bool Validate(InterfaceSettings d)
        {
            bool changed = false;
            if (!InterfaceFonts.IsKnown(Font)) { Font = d.Font; FontSize = d.FontSize; changed = true; }
            if (FontSize < InterfaceFonts.MinSize || FontSize > InterfaceFonts.MaxSize) { FontSize = InterfaceFonts.Find(Font).NativeSize; changed = true; }
            return changed;
        }
    }

    public sealed class GameplaySettings
    {
        public const int MinDrawDistance = 200;
        public const int MaxDrawDistance = 1000;
        public const int MinAutosaveMinutes = 1;
        public const int MaxAutosaveMinutes = 60;
#if FINAL
        public const bool DefaultAutosave = true;
#else
        public const bool DefaultAutosave = false;
#endif

        [JsonPropertyName("draw_distance")] public int DrawDistance { get; set; } = MaxDrawDistance; // 2..32
        [JsonPropertyName("language")] public string Language { get; set; } = "English";
        [JsonPropertyName("mouse_sensitivity")] public int MouseSensitivity { get; set; } = 100;
        [JsonPropertyName("invert_mouse_y")] public bool InvertMouseY { get; set; } = false;
        [JsonPropertyName("camera_sway")] public bool CameraSway { get; set; } = true;
        [JsonPropertyName("autosave")] public bool Autosave { get; set; } = DefaultAutosave;
        [JsonPropertyName("autosave_minutes")] public int AutosaveMinutes { get; set; } = 5;

        [JsonIgnore] public float AutosaveIntervalSeconds => Autosave ? AutosaveMinutes * 60f : 0f;

        public bool Validate(GameplaySettings d, HashSet<string> allowedLanguages = null)
        {
            bool changed = false;

            if (OutOfRange(DrawDistance, MinDrawDistance, MaxDrawDistance)) { DrawDistance = d.DrawDistance; changed = true; }
            if (OutOfRange(MouseSensitivity, 10, 300)) { MouseSensitivity = d.MouseSensitivity; changed = true; }
            if (OutOfRange(AutosaveMinutes, MinAutosaveMinutes, MaxAutosaveMinutes)) { AutosaveMinutes = d.AutosaveMinutes; changed = true; }

            if (string.IsNullOrWhiteSpace(Language)) { Language = d.Language; changed = true; }
            else if (allowedLanguages != null && allowedLanguages.Count > 0 && !allowedLanguages.Contains(Language))
            {
                Language = d.Language; changed = true;
            }

            return changed;
        }

        static bool OutOfRange(int v, int min, int max) => v < min || v > max;
    }

    public sealed class GameSettings
    {
        [JsonPropertyName("meta")] public MetaSettings Meta { get; set; } = new();
        [JsonPropertyName("audio")] public AudioSettings Audio { get; set; } = new();
        [JsonPropertyName("graphics")] public GraphicsSettings Graphics { get; set; } = new();
        [JsonPropertyName("interface")] public InterfaceSettings Interface { get; set; } = new();
        [JsonPropertyName("game")] public GameplaySettings Game { get; set; } = new();

        public static GameSettings Default() => new();

        public bool ValidateAgainst(GameSettings d, HashSet<string> allowedLanguages = null)
        {
            bool changed = false;

            if (Meta == null) { Meta = d.Meta; changed = true; }
            if (Audio == null) { Audio = d.Audio; changed = true; }
            if (Graphics == null) { Graphics = d.Graphics; changed = true; }
            if (Interface == null) { Interface = d.Interface; changed = true; }
            if (Game == null) { Game = d.Game; changed = true; }

            if (Meta.SchemaVersion <= 0) { Meta.SchemaVersion = d.Meta.SchemaVersion; changed = true; }

            changed |= Audio.Validate(d.Audio);
            changed |= Graphics.Validate(d.Graphics);
            changed |= Interface.Validate(d.Interface);
            changed |= Game.Validate(d.Game, allowedLanguages);

            return changed;
        }
    }

    public static class SettingsService
    {
        static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter() }
        };

        public static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Settings.json");

        public static GameSettings Load(IEnumerable<string> allowedLanguages = null)
        {
            return Load(FilePath, allowedLanguages);
        }

        public static GameSettings Load(string path, IEnumerable<string> allowedLanguages = null)
        {
            var defaults = GameSettings.Default();
            GameSettings loaded;

            if (!File.Exists(path))
            {
                Save( defaults);
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(path);
                loaded = JsonSerializer.Deserialize<GameSettings>(json, JsonOpts) ?? GameSettings.Default();
            }
            catch
            {
                loaded = GameSettings.Default();

                Debug.Error($"Failed to load game settings from {path}, using defaults.");
            }

            var langSet = allowedLanguages != null
                ? new HashSet<string>(allowedLanguages, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "English" };

            bool changed = loaded.ValidateAgainst(defaults, langSet);
            if (changed) Save( loaded);

            Debug.Success($"Game settings loaded from {path} (schema version: {loaded.Meta.SchemaVersion})");

            return loaded;
        }

        public static string Serialize(GameSettings settings)
        {
            return JsonSerializer.Serialize(settings, JsonOpts);
        }

        public static void Save(GameSettings settings)
        {
            string path = FilePath;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, Serialize(settings));



            Debug.Success($"Game settings saved to {path} (schema version: {settings.Meta.SchemaVersion})");
        }
    }
}
