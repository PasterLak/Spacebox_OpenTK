using Engine;
using Spacebox.Game.Generation.Tools.SaveData;
using Spacebox.Game.Player;

namespace Spacebox.Network.Players;

public static class PlayerSkinStore
{
    public const string FileName = "skins.nbt";

    private static readonly Dictionary<string, string> _skins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private static string _worldFolder;

    private sealed class SkinsFile
    {
        public Dictionary<string, string> Players { get; set; } = new Dictionary<string, string>();
    }

    public static void Load(string worldFolder)
    {
        _skins.Clear();
        _worldFolder = worldFolder;
        var path = PathOf(worldFolder);
        if (path == null || !SaveDataFile.Exists(path, null)) return;

        try
        {
            var file = SaveDataFile.Read<SkinsFile>(path, null);
            if (file?.Players == null) return;
            foreach (var pair in file.Players)
                if (!string.IsNullOrWhiteSpace(pair.Key) && AstronautSkins.TryNormalize(pair.Value, out var skin)) _skins[pair.Key] = skin;
        }
        catch (Exception ex)
        {
            Debug.Error($"[PlayerSkins] Failed to read {path}: {ex.Message}");
        }
    }

    public static string Resolve(string playerName, string chosenSkin)
    {
        if (!string.IsNullOrEmpty(playerName) && _skins.TryGetValue(playerName, out var skin)) return skin;
        return AstronautSkins.Normalize(chosenSkin);
    }

    public static void Set(string playerName, string skin)
    {
        if (string.IsNullOrWhiteSpace(playerName) || !AstronautSkins.TryNormalize(skin, out var name)) return;
        _skins[playerName] = name;
        Save();
    }

    public static void Reset()
    {
        _skins.Clear();
        _worldFolder = null;
    }

    private static void Save()
    {
        var path = PathOf(_worldFolder);
        if (path == null) return;

        try
        {
            SaveDataFile.Write(path, new SkinsFile { Players = new Dictionary<string, string>(_skins) }, null);
        }
        catch (Exception ex)
        {
            Debug.Error($"[PlayerSkins] Failed to write {path}: {ex.Message}");
        }
    }

    private static string PathOf(string worldFolder) => string.IsNullOrEmpty(worldFolder) ? null : Path.Combine(worldFolder, FileName);
}
