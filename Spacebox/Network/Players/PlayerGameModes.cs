using System.Text.Json;
using System.Text.Json.Nodes;
using Engine;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Network.Players;

public sealed class PlayerGameModes
{
    public const string FieldName = "GameMode";

    private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions { WriteIndented = true };

    private readonly Dictionary<string, GameMode?> _cache = new Dictionary<string, GameMode?>(StringComparer.OrdinalIgnoreCase);
    private string _folder;

    public static PlayerGameModes Current { get; } = new PlayerGameModes();

    public void Load(string worldFolder)
    {
        _cache.Clear();
        _folder = worldFolder;
    }

    public void Reset()
    {
        _cache.Clear();
        _folder = null;
    }

    public GameMode Get(string name, GameMode fallback) => TryGet(name, out var mode) ? mode : fallback;

    public bool TryGet(string name, out GameMode mode)
    {
        mode = default;
        if (string.IsNullOrWhiteSpace(name)) return false;

        string key = name.Trim();
        if (!_cache.TryGetValue(key, out var cached))
        {
            cached = ReadFromFile(key);
            _cache[key] = cached;
        }

        if (cached == null) return false;
        mode = cached.Value;
        return true;
    }

    public void Set(string name, GameMode mode)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        string key = name.Trim();
        _cache[key] = mode;
        WriteToFile(key, mode);
    }

    public string ApplyTo(string name, string playerJson)
    {
        if (string.IsNullOrEmpty(playerJson)) return playerJson;

        try
        {
            if (JsonNode.Parse(playerJson) is not JsonObject root) return playerJson;
            root.Remove(FieldName);
            if (TryGet(name, out var mode)) root[FieldName] = (int)mode;
            return root.ToJsonString(WriteOptions);
        }
        catch (JsonException)
        {
            return playerJson;
        }
    }

    public static bool TryRead(string playerJson, out GameMode mode)
    {
        mode = default;
        if (string.IsNullOrEmpty(playerJson)) return false;

        try
        {
            if (JsonNode.Parse(playerJson) is not JsonObject root || root[FieldName] is not JsonValue value) return false;
            if (!value.TryGetValue(out int raw) || raw < 0 || raw > (int)GameMode.Spectator) return false;
            mode = (GameMode)raw;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private GameMode? ReadFromFile(string name)
    {
        if (string.IsNullOrEmpty(_folder)) return null;

        try
        {
            string json = PlayerSaveLoadManager.ReadPlayerJson(_folder, name, false);
            return TryRead(json, out var mode) ? mode : null;
        }
        catch (IOException ex)
        {
            Debug.Error($"[GameModes] Failed to read the save of {name}: {ex.Message}");
            return null;
        }
    }

    private void WriteToFile(string name, GameMode mode)
    {
        if (string.IsNullOrEmpty(_folder)) return;

        string json = PlayerSaveLoadManager.ReadPlayerJson(_folder, name, false);
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            if (JsonNode.Parse(json) is not JsonObject root) return;
            root[FieldName] = (int)mode;
            PlayerSaveLoadManager.WritePlayerJson(_folder, name, root.ToJsonString(WriteOptions));
        }
        catch (JsonException ex)
        {
            Debug.Error($"[GameModes] Failed to update the save of {name}: {ex.Message}");
        }
    }
}
