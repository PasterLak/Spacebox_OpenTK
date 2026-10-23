namespace Spacebox.Editor.Data;

public sealed class CompositeEntrySource : IEntrySource, IReorderableEntrySource
{
    private readonly IReadOnlyList<IEntrySource> _sources;
    private readonly Func<IEntrySource> _target;
    private readonly List<EditorEntry> _entries = new List<EditorEntry>();
    private readonly List<string> _groups = new List<string>();
    private readonly Dictionary<EditorEntry, IEntrySource> _owners = new Dictionary<EditorEntry, IEntrySource>();
    private IReadOnlyList<EditorEntry>[] _seen;
    private int[] _seenCounts;

    public EntryDefinition Definition { get; }

    public CompositeEntrySource(EntryDefinition definition, Func<IEntrySource> target, params IEntrySource[] sources)
    {
        Definition = definition;
        _target = target;
        _sources = sources;
        _seen = new IReadOnlyList<EditorEntry>[sources.Length];
        _seenCounts = new int[sources.Length];
    }

    public IReadOnlyList<string> Groups
    {
        get
        {
            GetEntries();
            return _groups;
        }
    }

    public IReadOnlyList<EditorEntry> GetEntries()
    {
        bool changed = false;
        var lists = new IReadOnlyList<EditorEntry>[_sources.Count];
        for (int i = 0; i < _sources.Count; i++)
        {
            lists[i] = _sources[i].GetEntries();
            if (!ReferenceEquals(lists[i], _seen[i]) || lists[i].Count != _seenCounts[i] || Changed(lists[i], i)) changed = true;
        }
        if (!changed) return _entries;

        _entries.Clear();
        _owners.Clear();
        _groups.Clear();
        for (int i = 0; i < _sources.Count; i++)
        {
            _seen[i] = lists[i];
            _seenCounts[i] = lists[i].Count;
            foreach (var entry in lists[i])
            {
                _entries.Add(entry);
                _owners[entry] = _sources[i];
            }
            foreach (var group in _sources[i].Groups)
            {
                if (!_groups.Exists(g => string.Equals(g, group, StringComparison.OrdinalIgnoreCase))) _groups.Add(group);
            }
        }
        return _entries;
    }

    public EditorEntry Add(string group) => _target().Add(group);

    public EditorEntry Duplicate(EditorEntry entry)
    {
        var owner = OwnerOf(entry);
        if (owner is not EntryFolderSource { CanCreate: false }) return owner.Duplicate(entry);

        var copy = _target().Add(entry.Group);
        if (copy?.Node is System.Text.Json.Nodes.JsonObject target && entry.Node is System.Text.Json.Nodes.JsonObject source)
        {
            target.Clear();
            foreach (var property in source) target[property.Key] = property.Value?.DeepClone();
            FillProduct(target, ((EntryFolderSource)owner).OwnerId(entry));
            copy.File.MarkDirty();
        }
        return copy;
    }

    private static void FillProduct(System.Text.Json.Nodes.JsonObject blueprint, string ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId)) return;
        if (EditorJson.Get(blueprint, "Product") is not System.Text.Json.Nodes.JsonObject product)
        {
            EditorJson.Set(blueprint, "Product", new System.Text.Json.Nodes.JsonObject { ["Item"] = ownerId, ["Quantity"] = 1 });
            return;
        }
        if (string.IsNullOrWhiteSpace(EditorJson.GetString(product, "Item"))) EditorJson.Set(product, "Item", System.Text.Json.Nodes.JsonValue.Create(ownerId));
    }

    public void Remove(EditorEntry entry) => OwnerOf(entry).Remove(entry);

    public bool CanMove(EditorEntry entry, int delta) => OwnerOf(entry) is IReorderableEntrySource owner && owner.CanMove(entry, delta);

    public void Move(EditorEntry entry, int delta)
    {
        if (OwnerOf(entry) is IReorderableEntrySource owner) owner.Move(entry, delta);
    }

    private IEntrySource OwnerOf(EditorEntry entry)
    {
        GetEntries();
        if (_owners.TryGetValue(entry, out var owner)) return owner;
        foreach (var pair in _owners)
        {
            if (pair.Key.Node != null && ReferenceEquals(pair.Key.Node, entry.Node)) return pair.Value;
        }
        return _target();
    }

    private bool Changed(IReadOnlyList<EditorEntry> list, int index)
    {
        if (list.Count == 0) return false;
        return !_owners.ContainsKey(list[0]) || !_owners.ContainsKey(list[list.Count - 1]);
    }
}
