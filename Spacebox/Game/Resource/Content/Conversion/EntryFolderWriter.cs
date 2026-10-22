using System.Text.Json.Nodes;

namespace Spacebox.Game.Resource.Content.Conversion;

public sealed class EntryFolderWriter
{
    private readonly string _root;
    private readonly EntryKind _kind;
    private readonly ISet<string> _protectedEmissionNames;
    private readonly ConversionResult _result;
    private readonly string _folderRoot;
    private readonly Dictionary<string, string> _entryDirectories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public EntryFolderWriter(string root, EntryKind kind, ISet<string> protectedEmissionNames, ConversionResult result)
    {
        _root = root;
        _kind = kind;
        _protectedEmissionNames = protectedEmissionNames;
        _result = result;
        _folderRoot = Path.Combine(root, kind.Folder);
    }

    public IReadOnlyDictionary<string, string> EntryDirectories => _entryDirectories;

    public void Convert(IReadOnlyList<string> finalOrder)
    {
        foreach (var file in EntryFolderScanner.FindEntryFiles(_folderRoot, _kind.EntryFile))
        {
            if (ContentJson.ParseFile(file, null) is JsonObject obj)
                _entryDirectories.TryAdd(LayerContentReader.KeyOf(obj, "ID"), Path.GetDirectoryName(file));
        }

        string flatPath = Path.Combine(_root, _kind.FlatFile);
        if (!File.Exists(flatPath)) return;

        if (ContentJson.ParseFile(flatPath, _result.Errors) is not JsonArray array)
        {
            _result.Errors.Add($"'{flatPath}' is not an array");
            return;
        }

        var entries = new List<JsonObject>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in array)
        {
            if (node is not JsonObject obj) continue;
            string id = LayerContentReader.KeyOf(obj, "ID");
            if (string.IsNullOrEmpty(id))
            {
                _result.Errors.Add($"{_kind.Name} without ID in {_kind.FlatFile} cannot be converted");
                continue;
            }
            if (!seen.Add(id))
            {
                _result.Log.Add($"{_kind.Name} '{id}' is defined twice in {_kind.FlatFile}; the ignored copy was dropped");
                continue;
            }
            entries.Add(obj);
        }

        var textures = new TextureOwnership(_folderRoot, _entryDirectories.Values, entries);
        var emissions = EmissionFiles(Path.Combine(_root, GameSetFormat.EmissionsFolder));

        foreach (var entry in entries)
        {
            string id = LayerContentReader.KeyOf(entry, "ID");
            var owned = textures.OwnedBy(entry);
            string directory = _entryDirectories.TryGetValue(id, out var existing) ? existing : NewEntryDirectory(id, owned);
            Directory.CreateDirectory(directory);
            ContentJson.WriteFile(EntryFiles.PathFor(directory, _kind.EntryFile), entry);
            _entryDirectories[id] = directory;

            foreach (var texture in owned)
            {
                MoveFile(texture, Path.Combine(directory, Path.GetFileName(texture)));
                string name = AssetFolderScanner.NameOf(texture);
                if (emissions.TryGetValue(name, out var emission) && emission != null && !_protectedEmissionNames.Contains(name))
                    MoveFile(emission, Path.Combine(directory, GameSetFormat.EmissionsFolder, Path.GetFileName(emission)));
            }
        }

        if (finalOrder.Count > 0)
        {
            var order = new JsonArray();
            foreach (var id in finalOrder) order.Add(id);
            Directory.CreateDirectory(_folderRoot);
            ContentJson.WriteFile(Path.Combine(_folderRoot, GameSetFormat.OrderFile), order);
        }

        File.Delete(flatPath);
        _result.Log.Add($"{_kind.FlatFile}: {entries.Count} entries moved to {_kind.Folder}/");
    }

    private string NewEntryDirectory(string id, IReadOnlyList<string> owned)
    {
        string parent = owned.Count > 0 ? Path.GetDirectoryName(owned[0]) : _folderRoot;
        string candidate = Path.Combine(parent, id);
        int suffix = 2;
        while (Directory.Exists(candidate) || File.Exists(candidate)) candidate = Path.Combine(parent, $"{id}_{suffix++}");
        return candidate;
    }

    private void MoveFile(string from, string to)
    {
        if (string.Equals(Path.GetFullPath(from), Path.GetFullPath(to), StringComparison.OrdinalIgnoreCase) || File.Exists(to) || !File.Exists(from)) return;

        Directory.CreateDirectory(Path.GetDirectoryName(to));
        File.Move(from, to);
        _result.MovedFiles++;
    }

    private static Dictionary<string, string> EmissionFiles(string root)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(root)) return result;

        foreach (var file in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
        {
            if (!GameSetFormat.HasExtension(file, GameSetFormat.ImageExtensions) || GameSetFormat.IsIgnoredPath(root, file)) continue;
            string name = AssetFolderScanner.NameOf(file);
            result[name] = result.ContainsKey(name) ? null : file;
        }
        return result;
    }
}
