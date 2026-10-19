using System.Text.Json.Nodes;

namespace Spacebox.Editor.Data;

public sealed class FolderEntrySource : IEntrySource
{
    private readonly GameSetProject _project;
    private readonly string _relativeFolder;
    private readonly List<EditorEntry> _entries = new List<EditorEntry>();
    private int _version = -1;

    public EntryDefinition Definition { get; }
    public IReadOnlyList<string> Groups => Array.Empty<string>();

    public FolderEntrySource(GameSetProject project, string relativeFolder, EntryDefinition definition)
    {
        _project = project;
        _relativeFolder = relativeFolder;
        Definition = definition;
    }

    public IReadOnlyList<EditorEntry> GetEntries()
    {
        int version = _project.Version;
        if (version == _version) return _entries;

        var files = _project.GetFolderFiles(_relativeFolder);

        _version = version;
        _entries.Clear();
        for (int i = 0; i < files.Count; i++)
        {
            if (files[i].Root == null && files[i].Error == null) continue;
            _entries.Add(new EditorEntry(files[i].RelativePath, files[i].Root, files[i]));
        }
        return _entries;
    }

    public EditorEntry Add(string group)
    {
        var node = Definition.Template();
        var name = MakeFileName(Definition.NewIdPrefix);
        Definition.SetId(node, name);
        return Create(name, node);
    }

    public EditorEntry Duplicate(EditorEntry entry)
    {
        var node = EditorJson.Clone(entry.Node);
        var name = MakeFileName(Path.GetFileNameWithoutExtension(entry.File.RelativePath));
        if (!string.IsNullOrEmpty(Definition.GetId(node))) Definition.SetId(node, name);
        return Create(name, node);
    }

    public void Remove(EditorEntry entry)
    {
        _project.DeleteFile(entry.File);
        _version = -1;
    }

    public static string FileNameOf(EditorEntry entry)
    {
        return Path.GetFileNameWithoutExtension(entry.File.RelativePath);
    }

    private EditorEntry Create(string name, JsonNode node)
    {
        var file = _project.CreateFile(_relativeFolder.TrimEnd('/') + "/" + name + ".json", node);
        _version = -1;
        var entries = GetEntries();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].File == file) return entries[i];
        }
        return null;
    }

    private string MakeFileName(string baseName)
    {
        return EditorJson.MakeUniqueId(baseName, name => _project.Exists(_relativeFolder.TrimEnd('/') + "/" + name + ".json"));
    }
}
