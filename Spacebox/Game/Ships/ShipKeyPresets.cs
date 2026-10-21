using System.Text.Json;

namespace Spacebox.Game.Ships;

public sealed class ShipKeyPresets
{
    private readonly Dictionary<string, int[]> _keys = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase);

    public void Remember(string consoleId, IReadOnlyList<int> keys)
    {
        if (string.IsNullOrWhiteSpace(consoleId) || keys == null) return;

        var copy = new int[keys.Count];
        for (int i = 0; i < copy.Length; i++) copy[i] = keys[i];
        _keys[consoleId] = copy;
    }

    public bool TryGet(string consoleId, out int[] keys)
    {
        keys = null;
        return !string.IsNullOrWhiteSpace(consoleId) && _keys.TryGetValue(consoleId, out keys);
    }

    public static int[] WithKey(ConsoleBindings bindings, int group, int key)
    {
        var keys = new int[bindings.GroupCount];
        for (int i = 0; i < keys.Length; i++) keys[i] = i == group ? key : bindings.GetKey(i);
        return keys;
    }

    public string Serialize() => JsonSerializer.Serialize(_keys);

    public static ShipKeyPresets Deserialize(string json)
    {
        var presets = new ShipKeyPresets();
        if (string.IsNullOrWhiteSpace(json)) return presets;

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, int[]>>(json);
            if (data == null) return presets;
            foreach (var pair in data) presets.Remember(pair.Key, pair.Value);
        }
        catch (JsonException exception)
        {
            Engine.Debug.Warning($"[Ship] Saved console keys could not be read: {exception.Message}");
        }

        return presets;
    }
}
