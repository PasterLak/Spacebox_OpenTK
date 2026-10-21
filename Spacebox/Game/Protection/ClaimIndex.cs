using System.Text.Json;
using Engine;

namespace Spacebox.Game.Protection;

public sealed class ClaimIndex
{
    public const string FileName = "claims.json";

    public sealed class Entry
    {
        public string Owner { get; set; } = "";
        public bool Active { get; set; }
    }

    private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
    private string _folder;

    public int Count => _entries.Count;
    public IReadOnlyDictionary<string, Entry> Entries => _entries;

    public static string KeyFor(long entityId, OpenTK.Mathematics.Vector3i cell) => $"{entityId}:{cell.X}:{cell.Y}:{cell.Z}";

    public bool Upsert(string key, string owner, bool active)
    {
        if (_entries.TryGetValue(key, out var entry) && ClaimMembership.SameName(entry.Owner, owner) && entry.Active == active) return false;

        _entries[key] = new Entry { Owner = ClaimMembership.Sanitize(owner), Active = active };
        Save();
        return true;
    }

    public bool Remove(string key)
    {
        if (!_entries.Remove(key)) return false;
        Save();
        return true;
    }

    public int CountActive(string owner) => CountFor(owner, true);

    public int CountAll(string owner) => CountFor(owner, false);

    public void Load(string worldFolder)
    {
        _entries.Clear();
        _folder = worldFolder;
        string path = PathFor(worldFolder);
        if (path == null || !File.Exists(path)) return;

        try
        {
            var loaded = JsonSerializer.Deserialize<Dictionary<string, Entry>>(File.ReadAllText(path));
            if (loaded == null) return;
            foreach (var pair in loaded)
            {
                if (pair.Value != null && !string.IsNullOrEmpty(pair.Key)) _entries[pair.Key] = pair.Value;
            }
        }
        catch (Exception ex)
        {
            Debug.Error($"[Claims] Failed to read {FileName}: {ex.Message}");
        }
    }

    public void Reset()
    {
        _entries.Clear();
        _folder = null;
    }

    private int CountFor(string owner, bool activeOnly)
    {
        int count = 0;
        foreach (var entry in _entries.Values)
        {
            if ((!activeOnly || entry.Active) && ClaimMembership.SameName(entry.Owner, owner)) count++;
        }
        return count;
    }

    private void Save()
    {
        string path = PathFor(_folder);
        if (path == null) return;

        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(_entries));
        }
        catch (Exception ex)
        {
            Debug.Error($"[Claims] Failed to write {FileName}: {ex.Message}");
        }
    }

    private static string PathFor(string folder) => string.IsNullOrEmpty(folder) ? null : Path.Combine(folder, FileName);
}
