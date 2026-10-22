namespace Spacebox.Game.Resource.Content;

public sealed class KeyedContentList
{
    private readonly List<ContentEntry> _entries = new List<ContentEntry>();
    private readonly Dictionary<string, int> _indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private readonly string _kind;
    private readonly ICollection<string> _warnings;

    public KeyedContentList(string kind, ICollection<string> warnings)
    {
        _kind = kind;
        _warnings = warnings;
    }

    public IReadOnlyList<ContentEntry> Entries => _entries;

    public bool Contains(string key) => !string.IsNullOrEmpty(key) && _indices.ContainsKey(key);

    public void Add(ContentEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Key))
        {
            _entries.Add(entry);
            return;
        }

        if (!_indices.TryGetValue(entry.Key, out int index))
        {
            _indices.Add(entry.Key, _entries.Count);
            _entries.Add(entry);
            return;
        }

        var existing = _entries[index];
        if (ReferenceEquals(existing.Layer, entry.Layer))
        {
            _warnings?.Add($"{_kind} '{entry.Key}' is defined twice in '{entry.Layer.Id}' ({existing.Source} and {entry.Source}); the first one is used");
            return;
        }

        _entries[index] = existing.PatchedBy(entry);
    }
}
