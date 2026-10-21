using System.Text.Json;
using Engine;
using Spacebox.Game.Mobs;

namespace Spacebox.Game.Rules;

public static class WorldRulesStore
{
    public const string FileName = "rules.json";

    private static string _worldFolder;

    private sealed class WorldRulesFile
    {
        public ServerRules Gameplay { get; set; } = new ServerRules();
        public bool FriendlyMobs { get; set; }
    }

    public static void Load(string worldFolder)
    {
        _worldFolder = worldFolder;
        var path = PathOf(worldFolder);
        if (path == null || !File.Exists(path)) return;

        try
        {
            var file = JsonSerializer.Deserialize<WorldRulesFile>(File.ReadAllText(path));
            if (file == null) return;
            file.Gameplay ??= new ServerRules();
            file.Gameplay.Validate();
            ServerRules.Session.CopyFrom(file.Gameplay);
            MobRules.FriendlyMobs = file.FriendlyMobs;
        }
        catch (Exception ex)
        {
            Debug.Error($"[WorldRules] Failed to read {path}: {ex.Message}");
        }
    }

    public static void Save()
    {
        var path = PathOf(_worldFolder);
        if (path == null) return;

        try
        {
            var file = new WorldRulesFile { Gameplay = ServerRules.Session, FriendlyMobs = MobRules.FriendlyMobs };
            File.WriteAllText(path, JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Debug.Error($"[WorldRules] Failed to write {path}: {ex.Message}");
        }
    }

    public static void Reset() => _worldFolder = null;

    private static string PathOf(string worldFolder) => string.IsNullOrEmpty(worldFolder) ? null : Path.Combine(worldFolder, FileName);
}
