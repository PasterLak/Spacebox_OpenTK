namespace Spacebox.Game.Resource.Content;

public sealed class KeyedContentList
{
    private readonly List<ContentEntry> _entries = new List<ContentEntry>();
    private readonly Dictionary<string, int> _indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private readonly string _kind;
    private readonly ICollection<string> _warnings;
    private readonly Func<ContentLayer, bool> _isBase;
    private readonly Dictionary<string, ContentLayer> _definedBy = new Dictionary<string, ContentLayer>(StringComparer.OrdinalIgnoreCase);

    public KeyedContentList(string kind, ICollection<string> warnings, Func<ContentLayer, bool> isBase = null)
    {
        _kind = kind;
        _warnings = warnings;
        _isBase = isBase;
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
            _definedBy[entry.Key] = entry.Layer;
            return;
        }

        var existing = _entries[index];
        if (ReferenceEquals(existing.Layer, entry.Layer))
        {
            _warnings?.Add($"{_kind} '{entry.Key}' is defined twice in '{entry.Layer.Id}' ({existing.Source} and {entry.Source}); the first one is used");
            return;
        }

        if (_isBase != null && _definedBy.TryGetValue(entry.Key, out var owner) && !_isBase(owner) && !ReferenceEquals(owner, entry.Layer))
            _warnings?.Add($"{_kind} '{entry.Key}' from extension '{owner.Id}' is changed by extension '{entry.Layer.Id}'; use your own id prefix if this is a different {_kind.ToLowerInvariant()}");

        _entries[index] = existing.PatchedBy(entry);
    }
}
