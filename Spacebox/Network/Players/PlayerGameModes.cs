using System.Text.Json;
using Engine;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Network.Players;

public sealed class PlayerGameModes
{
    public const string FileName = "playermodes.json";

    private readonly Dictionary<string, GameMode> _modes = new Dictionary<string, GameMode>(StringComparer.OrdinalIgnoreCase);
    private string _folder;

    public static PlayerGameModes Current { get; } = new PlayerGameModes();

    public int Count => _modes.Count;

    public void Load(string worldFolder)
    {
        _modes.Clear();
        _folder = worldFolder;
        string path = PathFor(worldFolder);
        if (path == null || !File.Exists(path)) return;

        try
        {
            var loaded = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(path));
            if (loaded == null) return;
            foreach (var pair in loaded)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value < 0 || pair.Value > (int)GameMode.Spectator) continue;
                _modes[pair.Key.Trim()] = (GameMode)pair.Value;
            }
        }
        catch (Exception ex)
        {
            Debug.Error($"[GameModes] Failed to read {FileName}: {ex.Message}");
        }
    }

    public void Reset()
    {
        _modes.Clear();
        _folder = null;
    }

    public GameMode Get(string name, GameMode fallback)
    {
        if (string.IsNullOrWhiteSpace(name)) return fallback;
        return _modes.TryGetValue(name.Trim(), out var mode) ? mode : fallback;
    }

    public bool TryGet(string name, out GameMode mode)
    {
        mode = default;
        return !string.IsNullOrWhiteSpace(name) && _modes.TryGetValue(name.Trim(), out mode);
    }

    public void Set(string name, GameMode mode)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        _modes[name.Trim()] = mode;
        Save();
    }

    private void Save()
    {
        string path = PathFor(_folder);
        if (path == null) return;

        try
        {
            var data = new Dictionary<string, int>();
            foreach (var pair in _modes) data[pair.Key] = (int)pair.Value;
            File.WriteAllText(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Debug.Error($"[GameModes] Failed to write {FileName}: {ex.Message}");
        }
    }

    private static string PathFor(string folder) => string.IsNullOrEmpty(folder) ? null : Path.Combine(folder, FileName);
}
