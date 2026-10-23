using System.Text.Json.Nodes;

namespace Spacebox.Editor.Data;

public sealed class GroupedArrayEntrySource : IEntrySource
{
    private readonly GameSetProject _project;
    private readonly string _relativeFile;
    private readonly string _groupsKey;
    private readonly string _groupNameKey;
    private readonly string _itemsKey;
    private readonly List<EditorEntry> _entries = new List<EditorEntry>();
    private readonly List<string> _groups = new List<string>();
    private int _version = -1;

    public EntryDefinition Definition { get; }
    public IReadOnlyList<string> Groups
    {
        get
        {
            GetEntries();
            return _groups;
        }
    }

    public GroupedArrayEntrySource(GameSetProject project, string relativeFile, string groupsKey, string groupNameKey, string itemsKey, EntryDefinition definition)
    {
        _project = project;
        _relativeFile = relativeFile;
        _groupsKey = groupsKey;
        _groupNameKey = groupNameKey;
        _itemsKey = itemsKey;
        Definition = definition;
    }

    public IReadOnlyList<EditorEntry> GetEntries()
    {
        var file = _project.GetFile(_relativeFile);
        if (file.Version == _version) return _entries;

        _version = file.Version;
        _entries.Clear();
        _groups.Clear();

        var groups = EditorJson.GetArray(file.Root, _groupsKey);
        if (groups == null) return _entries;

        for (int g = 0; g < groups.Count; g++)
        {
            if (groups[g] is not JsonObject group) continue;
            var name = EditorJson.GetString(group, _groupNameKey, "group" + g);
            _groups.Add(name);

            if (EditorJson.Get(group, _itemsKey) is not JsonArray items) continue;
            for (int i = 0; i < items.Count; i++)
                _entries.Add(new EditorEntry(_relativeFile + "#" + g + "#" + i, items[i], file, name));
        }

        return _entries;
    }

    public EditorEntry Add(string group)
    {
        var items = EnsureItems(string.IsNullOrWhiteSpace(group) ? "new_group" : group);
        if (items == null) return null;
        var node = Definition.Template();
        items.Add(node);
        return Commit(node);
    }

    public EditorEntry Duplicate(EditorEntry entry)
    {
        var items = EnsureItems(entry.Group);
        if (items == null) return null;
        var node = EditorJson.Clone(entry.Node);
        int index = IndexOf(items, entry.Node);
        items.Insert(index < 0 ? items.Count : index + 1, node);
        return Commit(node);
    }

    public void Remove(EditorEntry entry)
    {
        var items = FindItems(entry.Group);
        int index = items == null ? -1 : IndexOf(items, entry.Node);
        if (index < 0) return;
        items.RemoveAt(index);
        _project.GetFile(_relativeFile).MarkDirty();
    }

    private EditorEntry Commit(JsonNode node)
    {
        _project.GetFile(_relativeFile).MarkDirty();
        var entries = GetEntries();
        for (int i = 0; i < entries.Count; i++)
        {
            if (ReferenceEquals(entries[i].Node, node)) return entries[i];
        }
        return null;
    }

    private JsonArray FindItems(string groupName)
    {
        var groups = EditorJson.GetArray(_project.GetFile(_relativeFile).Root, _groupsKey);
        if (groups == null) return null;
        foreach (var node in groups)
        {
            if (node is JsonObject group && string.Equals(EditorJson.GetString(group, _groupNameKey), groupName, StringComparison.OrdinalIgnoreCase))
                return EditorJson.GetArray(group, _itemsKey);
        }
        return null;
    }

    private JsonArray EnsureItems(string groupName)
    {
        var file = _project.GetFile(_relativeFile);
        if (file.Error != null) return null;
        if (file.Root is not JsonObject root)
        {
            root = new JsonObject();
            file.Root = root;
        }

        if (EditorJson.Get(root, _groupsKey) is not JsonArray groups)
        {
            groups = new JsonArray();
            EditorJson.Set(root, _groupsKey, groups);
        }

        foreach (var node in groups)
        {
            if (node is JsonObject group && string.Equals(EditorJson.GetString(group, _groupNameKey), groupName, StringComparison.OrdinalIgnoreCase))
                return EnsureArray(group);
        }

        var created = new JsonObject { [_groupNameKey] = groupName };
        groups.Add(created);
        return EnsureArray(created);
    }

    private JsonArray EnsureArray(JsonObject group)
    {
        if (EditorJson.Get(group, _itemsKey) is JsonArray items) return items;
        items = new JsonArray();
        EditorJson.Set(group, _itemsKey, items);
        return items;
    }

    private static int IndexOf(JsonArray array, JsonNode node)
    {
        for (int i = 0; i < array.Count; i++)
        {
            if (ReferenceEquals(array[i], node)) return i;
        }
        return -1;
    }
}
