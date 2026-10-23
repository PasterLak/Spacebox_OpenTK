using System.Text.Json.Nodes;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Editor.Data;

public sealed class EntryFolderSource : IEntrySource
{
    private readonly GameSetProject _project;
    private readonly string[] _roots;
    private readonly string _entryFile;
    private readonly bool _canCreate;
    private readonly List<EditorEntry> _entries = new List<EditorEntry>();
    private int _version = -1;

    public EntryDefinition Definition { get; }
    public IReadOnlyList<string> Groups => Array.Empty<string>();

    public EntryFolderSource(GameSetProject project, string root, string entryFile, EntryDefinition definition)
        : this(project, new[] { root }, entryFile, definition, true)
    {
    }

    public EntryFolderSource(GameSetProject project, string[] roots, string entryFile, EntryDefinition definition, bool canCreate)
    {
        _project = project;
        _roots = roots;
        _entryFile = entryFile;
        _canCreate = canCreate;
        Definition = definition;
    }

    public bool CanCreate => _canCreate;

    public IReadOnlyList<EditorEntry> GetEntries()
    {
        int version = _project.Version;
        if (version == _version) return _entries;

        _version = version;
        _entries.Clear();
        var files = _project.ListJsonFiles();
        foreach (var root in _roots)
        {
            var paths = new List<string>();
            foreach (var path in files)
            {
                if (IsEntryFile(path, root)) paths.Add(path);
            }

            var ordered = EntryFolderScanner.ApplyOrder(paths, IdOf, ReadOrder(root));
            foreach (var path in ordered)
            {
                var file = _project.GetFile(path);
                if (file.Root == null && file.Error == null) continue;
                _entries.Add(new EditorEntry(path, file.Root, file));
            }
        }
        return _entries;
    }

    public EditorEntry Add(string group)
    {
        var node = Definition.Template();
        string id = MakeId(Definition.NewIdPrefix);
        Definition.SetId(node, id);
        Definition.PrepareNew(node, GetEntries());
        return Create(id, node);
    }

    public EditorEntry Duplicate(EditorEntry entry)
    {
        var node = EditorJson.Clone(entry.Node);
        var id = Definition.GetId(entry.Node);
        id = MakeId(string.IsNullOrEmpty(id) ? Definition.NewIdPrefix : id);
        Definition.SetId(node, id);
        Definition.PrepareNew(node, GetEntries());
        return Create(id, node);
    }

    private static readonly string[] SiblingFiles = { GameSetFormat.BlueprintFile, GameSetFormat.EntryRecipesFile };

    public void Remove(EditorEntry entry)
    {
        string root = RootOf(entry.File.RelativePath);
        string id = Definition.GetId(entry.Node);
        if (_canCreate) DeleteSiblings(entry.File.RelativePath);
        _project.DeleteFile(entry.File);
        if (_canCreate && root != null) RemoveFromOrder(root, id);
        _version = -1;
    }

    private void DeleteSiblings(string entryPath)
    {
        string folder = Path.GetDirectoryName(entryPath)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(folder)) return;
        foreach (var name in SiblingFiles)
        {
            string path = folder + "/" + name;
            if (_project.Exists(path)) _project.DeleteFile(_project.GetFile(path));
        }
    }

    private void RemoveFromOrder(string root, string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        string path = root.TrimEnd('/') + "/" + GameSetFormat.OrderFile;
        if (!_project.Exists(path)) return;

        var file = _project.GetFile(path);
        if (file.Root is not JsonArray order) return;
        bool removed = false;
        for (int i = order.Count - 1; i >= 0; i--)
        {
            if (order[i] is JsonValue value && value.TryGetValue(out string orderId) && string.Equals(orderId, id, StringComparison.OrdinalIgnoreCase))
            {
                order.RemoveAt(i);
                removed = true;
            }
        }
        if (removed) file.MarkDirty();
    }

    private string RootOf(string path)
    {
        foreach (var root in _roots)
        {
            if (IsEntryFile(path, root)) return root;
        }
        return null;
    }

    public bool Owns(EditorEntry entry)
    {
        return RootOf(entry.File.RelativePath) != null;
    }

    public string OwnerId(EditorEntry entry)
    {
        string folder = Path.GetDirectoryName(entry.File.RelativePath)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(folder)) return null;

        string folderName = folder.Substring(folder.LastIndexOf('/') + 1);
        foreach (var name in new[] { EntryFiles.NamedFile(folderName), GameSetFormat.BlockEntryFile, GameSetFormat.ItemEntryFile })
        {
            string path = folder + "/" + name;
            if (string.Equals(path, entry.File.RelativePath, StringComparison.OrdinalIgnoreCase) || !_project.Exists(path)) continue;
            string id = EditorJson.GetString(_project.GetFile(path).Root as JsonObject, "ID");
            if (!string.IsNullOrWhiteSpace(id)) return id;
        }
        return null;
    }

    private EditorEntry Create(string id, JsonNode node)
    {
        string name = FolderName(id);
        string folder = _roots[0].TrimEnd('/') + "/" + name;
        var file = _project.CreateFile(folder + "/" + EntryFiles.NamedFile(name), node);
        AppendToOrder(id);
        _version = -1;
        foreach (var entry in GetEntries())
        {
            if (entry.File == file) return entry;
        }
        return null;
    }

    private void AppendToOrder(string id)
    {
        string path = _roots[0].TrimEnd('/') + "/" + GameSetFormat.OrderFile;
        if (!_project.Exists(path)) return;

        var file = _project.GetFile(path);
        if (file.Root is not JsonArray order) return;
        order.Add(JsonValue.Create(id));
        file.MarkDirty();
    }

    private string MakeId(string baseId)
    {
        return EditorJson.MakeUniqueId(baseId, id =>
        {
            foreach (var entry in GetEntries())
            {
                if (string.Equals(Definition.GetId(entry.Node), id, StringComparison.OrdinalIgnoreCase)) return true;
            }
            string name = FolderName(id);
            string folder = _roots[0].TrimEnd('/') + "/" + name + "/";
            return _project.Exists(folder + EntryFiles.NamedFile(name)) || _project.Exists(folder + _entryFile);
        });
    }

    public static string FolderName(string id)
    {
        string shortId = id ?? "";
        int colon = shortId.LastIndexOf(':');
        if (colon >= 0) shortId = shortId.Substring(colon + 1);
        string name = Spacebox.Game.Resource.Content.Conversion.RecipeFilesWriter.SafeFileName(shortId).TrimStart('_');
        return name.Length == 0 ? "unknown" : name;
    }

    private string IdOf(string path)
    {
        var root = _project.GetFile(path).Root as JsonObject;
        return EditorJson.GetString(root, "ID");
    }

    private List<string> ReadOrder(string root)
    {
        var order = new List<string>();
        string path = root.TrimEnd('/') + "/" + GameSetFormat.OrderFile;
        if (!_project.Exists(path) || _project.GetFile(path).Root is not JsonArray array) return order;

        foreach (var node in array)
        {
            if (node is JsonValue value && value.TryGetValue(out string id)) order.Add(id);
        }
        return order;
    }

    private bool IsEntryFile(string path, string root)
    {
        string prefix = root.TrimEnd('/') + "/";
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        if (!EntryFiles.IsEntryFile(path, _entryFile, _project.Exists)) return false;

        var segments = path.Substring(prefix.Length).Split('/');
        if (segments.Length < 2) return false;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i].StartsWith('_') || string.Equals(segments[i], GameSetFormat.EmissionsFolder, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }
}
