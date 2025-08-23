using Engine;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Spacebox.Game
{
    public enum WindowMode { Fullscreen, Borderless, Windowed}

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
        public WindowMode WindowMode { get; set; } = WindowMode.Fullscreen;

        [JsonPropertyName("vsync")] public bool VSync { get; set; } = true;
        [JsonPropertyName("ambient_occlusion")] public bool AO { get; set; } = true;
        [JsonPropertyName("voxel_lighting")] public bool VoxelLighting { get; set; } = true;
        [JsonPropertyName("post_processing")] public bool PostProcessing { get; set; } = true;
        [JsonPropertyName("shadows")] public bool Shadows { get; set; } = true;
        [JsonPropertyName("effects_enabled")] public bool EffectsEnabled { get; set; } = true;
        [JsonPropertyName("fov")] public int Fov { get; set; } = 90;                           // 50..120
        [JsonPropertyName("resolution_scale_percent")] public int ResolutionScalePercent { get; set; } = 100; // 10..100

        public bool Validate(GraphicsSettings d)
        {
            bool changed = false;
            if (OutOfRange(Fov, 50, 120)) { Fov = d.Fov; changed = true; }
            if (OutOfRange(ResolutionScalePercent, 10, 100)) { ResolutionScalePercent = d.ResolutionScalePercent; changed = true; }
            return changed;
        }

        static bool OutOfRange(int v, int min, int max) => v < min || v > max;
    }

    public sealed class GameplaySettings
    {
        [JsonPropertyName("draw_distance")] public int DrawDistance { get; set; } = 12; // 2..32
        [JsonPropertyName("language")] public string Language { get; set; } = "English";
        [JsonPropertyName("keep_inventory")] public bool KeepInventory { get; set; } = false;

        public bool Validate(GameplaySettings d, HashSet<string> allowedLanguages = null)
        {
            bool changed = false;

            if (OutOfRange(DrawDistance, 2, 32)) { DrawDistance = d.DrawDistance; changed = true; }

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
        [JsonPropertyName("game")] public GameplaySettings Game { get; set; } = new();

        public static GameSettings Default() => new();

        public bool ValidateAgainst(GameSettings d, HashSet<string> allowedLanguages = null)
        {
            bool changed = false;

            if (Meta == null) { Meta = d.Meta; changed = true; }
            if (Audio == null) { Audio = d.Audio; changed = true; }
            if (Graphics == null) { Graphics = d.Graphics; changed = true; }
            if (Game == null) { Game = d.Game; changed = true; }

            if (Meta.SchemaVersion <= 0) { Meta.SchemaVersion = d.Meta.SchemaVersion; changed = true; }

            changed |= Audio.Validate(d.Audio);
            changed |= Graphics.Validate(d.Graphics);
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

        public static GameSettings Load(string path, IEnumerable<string> allowedLanguages = null)
        {
            var defaults = GameSettings.Default();
            GameSettings loaded;

            Debug.Warning($"Loading game settings from {path}...");

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

        public static void Save(GameSettings settings)
        {
            var p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
            string path = Path.Combine(p, "Settings.json");
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(settings, JsonOpts);
            File.WriteAllText(path, json);

            Debug.Success($"Game settings saved to {path} (schema version: {settings.Meta.SchemaVersion})");
        }
    }
}
