using System.Text.Json.Nodes;

namespace Spacebox.Editor.Data;

public sealed class ArrayEntrySource : IEntrySource, IReorderableEntrySource
{
    private readonly GameSetProject _project;
    private readonly string _relativeFile;
    private readonly string _arrayKey;
    private readonly List<EditorEntry> _entries = new List<EditorEntry>();
    private int _version = -1;

    public EntryDefinition Definition { get; }
    public IReadOnlyList<string> Groups => Array.Empty<string>();

    public ArrayEntrySource(GameSetProject project, string relativeFile, string arrayKey, EntryDefinition definition)
    {
        _project = project;
        _relativeFile = relativeFile;
        _arrayKey = arrayKey;
        Definition = definition;
    }

    public IReadOnlyList<EditorEntry> GetEntries()
    {
        var file = _project.GetFile(_relativeFile);
        if (file.Version == _version) return _entries;

        _version = file.Version;
        _entries.Clear();
        var array = EditorJson.GetArray(file.Root, _arrayKey);
        if (array == null) return _entries;

        for (int i = 0; i < array.Count; i++)
            _entries.Add(new EditorEntry(_relativeFile + "#" + i, array[i], file));

        return _entries;
    }

    public EditorEntry Add(string group)
    {
        var node = Definition.Template();
        Definition.SetId(node, MakeId(Definition.NewIdPrefix));
        Definition.PrepareNew(node, GetEntries());
        return Insert(node, -1);
    }

    public EditorEntry Duplicate(EditorEntry entry)
    {
        var node = EditorJson.Clone(entry.Node);
        var id = Definition.GetId(entry.Node);
        Definition.SetId(node, MakeId(string.IsNullOrEmpty(id) ? Definition.NewIdPrefix : id));
        Definition.PrepareNew(node, GetEntries());
        return Insert(node, IndexOf(entry) + 1);
    }

    public void Remove(EditorEntry entry)
    {
        var array = FindArray();
        int index = IndexOf(entry);
        if (array == null || index < 0) return;

        array.RemoveAt(index);
        _project.GetFile(_relativeFile).MarkDirty();
    }

    public bool CanMove(EditorEntry entry, int delta)
    {
        int index = IndexOf(entry);
        int target = index + delta;
        var array = FindArray();
        return array != null && index >= 0 && target >= 0 && target < array.Count;
    }

    public void Move(EditorEntry entry, int delta)
    {
        if (!CanMove(entry, delta)) return;

        var array = FindArray();
        int index = IndexOf(entry);
        var node = array[index];
        array.RemoveAt(index);
        array.Insert(index + delta, node);
        _project.GetFile(_relativeFile).MarkDirty();
    }

    private EditorEntry Insert(JsonNode node, int index)
    {
        var array = EnsureArray();
        if (array == null) return null;
        if (index < 0 || index > array.Count) index = array.Count;
        array.Insert(index, node);
        _project.GetFile(_relativeFile).MarkDirty();
        GetEntries();
        return _entries[index];
    }

    private int IndexOf(EditorEntry entry)
    {
        var array = FindArray();
        if (array == null) return -1;
        for (int i = 0; i < array.Count; i++)
        {
            if (ReferenceEquals(array[i], entry.Node)) return i;
        }
        return -1;
    }

    private JsonArray FindArray()
    {
        return EditorJson.GetArray(_project.GetFile(_relativeFile).Root, _arrayKey);
    }

    private JsonArray EnsureArray()
    {
        var file = _project.GetFile(_relativeFile);
        if (file.Error != null) return null;
        if (string.IsNullOrEmpty(_arrayKey))
        {
            if (file.Root is not JsonArray rootArray)
            {
                rootArray = new JsonArray();
                file.Root = rootArray;
            }
            return rootArray;
        }

        if (file.Root is not JsonObject obj)
        {
            obj = new JsonObject();
            file.Root = obj;
        }

        if (EditorJson.Get(obj, _arrayKey) is not JsonArray array)
        {
            array = new JsonArray();
            EditorJson.Set(obj, _arrayKey, array);
        }
        return array;
    }

    private string MakeId(string baseId)
    {
        var entries = GetEntries();
        return EditorJson.MakeUniqueId(baseId, id =>
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(Definition.GetId(entries[i].Node), id, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        });
    }
}
