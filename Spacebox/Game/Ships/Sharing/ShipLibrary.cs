using Engine;

namespace Spacebox.Game.Ships.Sharing;

public sealed class ShipLibraryEntry
{
    public ShipLibraryEntry(string path, DateTime written, ShipFile ship, string error)
    {
        Path = path;
        Written = written;
        Ship = ship;
        Error = error;
    }

    public string Path { get; }
    public DateTime Written { get; }
    public ShipFile Ship { get; }
    public string Error { get; }
    public string Title => Ship != null && !string.IsNullOrWhiteSpace(Ship.Name) ? Ship.Name : System.IO.Path.GetFileNameWithoutExtension(Path);
}

public sealed class ShipLibrary
{
    private readonly string _folder;
    private readonly Dictionary<string, ShipLibraryEntry> _cache = new Dictionary<string, ShipLibraryEntry>(StringComparer.OrdinalIgnoreCase);
    private readonly List<ShipLibraryEntry> _entries = new List<ShipLibraryEntry>();

    public ShipLibrary(string folder)
    {
        _folder = folder;
    }

    public string Folder => _folder;
    public IReadOnlyList<ShipLibraryEntry> Entries => _entries;

    public void Refresh()
    {
        _entries.Clear();
        if (!Directory.Exists(_folder)) return;

        foreach (var path in Directory.EnumerateFiles(_folder, "*" + ShipFile.Extension))
        {
            var written = File.GetLastWriteTimeUtc(path);
            if (!_cache.TryGetValue(path, out var entry) || entry.Written != written)
            {
                entry = Load(path, written);
                _cache[path] = entry;
            }
            _entries.Add(entry);
        }

        _entries.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
    }

    private static ShipLibraryEntry Load(string path, DateTime written)
    {
        try
        {
            return new ShipLibraryEntry(path, written, ShipFileCodec.Read(path), null);
        }
        catch (Exception ex)
        {
            Debug.Warning($"[Ships] {System.IO.Path.GetFileName(path)} could not be read: {ex.Message}");
            return new ShipLibraryEntry(path, written, null, ex.Message);
        }
    }
}
