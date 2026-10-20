namespace Spacebox.Game.Versioning;

public sealed class IdRemap
{
    private readonly Dictionary<string, string> _map = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly HashSet<string> _removed = new HashSet<string>(StringComparer.Ordinal);

    public int Count => _map.Count;
    public bool IsEmpty => _map.Count == 0;
    public int Changes { get; private set; }

    public IdRemap Rename(string from, string to)
    {
        Validate(from);
        Validate(to);
        _map[from] = to;
        _removed.Remove(from);
        return this;
    }

    public IdRemap Remove(string id, string replacement)
    {
        Validate(id);
        _map[id] = replacement;
        _removed.Add(id);
        return this;
    }

    public IdRemap WithRemovalsAs(string removedReplacement)
    {
        var copy = new IdRemap();
        foreach (var pair in _map)
        {
            if (_removed.Contains(pair.Key)) copy.Remove(pair.Key, removedReplacement);
            else copy.Rename(pair.Key, pair.Value);
        }
        return copy;
    }

    public bool Contains(string id)
    {
        return id != null && _map.ContainsKey(id);
    }

    public bool TryMap(string id, out string result)
    {
        result = id;
        if (id == null || !_map.TryGetValue(id, out var mapped)) return false;

        result = mapped;
        Changes++;
        return true;
    }

    private static void Validate(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !id.Contains(':'))
            throw new ArgumentException($"'{id}' must be a full id like 'default:light_rock'");
    }
}
