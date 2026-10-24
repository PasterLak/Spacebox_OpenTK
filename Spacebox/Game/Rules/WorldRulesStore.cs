using Spacebox.Game.Generation.Tools.SaveData;
using System.Text.Json;
using Engine;
using Spacebox.Game.Mobs;

namespace Spacebox.Game.Rules;

public sealed class WorldRulesStore
{
    public const string FileName = "rules.nbt";
    public const string LegacyFileName = "rules.json";

    private string _worldFolder;

    private sealed class WorldRulesFile
    {
        public ServerRules Gameplay { get; set; } = new ServerRules();
        public bool FriendlyMobs { get; set; }
    }

    public void Load(string worldFolder)
    {
        _worldFolder = worldFolder;
        var path = PathOf(worldFolder);
        if (path == null || !SaveDataFile.Exists(path, LegacyPathOf(worldFolder))) return;

        try
        {
            var file = SaveDataFile.Read<WorldRulesFile>(path, LegacyPathOf(worldFolder));
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

    public void Save()
    {
        var path = PathOf(_worldFolder);
        if (path == null) return;

        try
        {
            var file = new WorldRulesFile { Gameplay = ServerRules.Session, FriendlyMobs = MobRules.FriendlyMobs };
            SaveDataFile.Write(path, file, LegacyPathOf(_worldFolder));
        }
        catch (Exception ex)
        {
            Debug.Error($"[WorldRules] Failed to write {path}: {ex.Message}");
        }
    }

    private static string PathOf(string worldFolder) => string.IsNullOrEmpty(worldFolder) ? null : Path.Combine(worldFolder, FileName);

    private static string LegacyPathOf(string worldFolder) => string.IsNullOrEmpty(worldFolder) ? null : Path.Combine(worldFolder, LegacyFileName);
}
