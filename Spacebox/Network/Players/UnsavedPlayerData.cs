namespace Spacebox.Network.Players;

public sealed class UnsavedPlayerData
{
    private readonly Dictionary<string, string> _json = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public int Count => _json.Count;

    public void Store(string playerName, string json)
    {
        if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(json)) return;
        _json[playerName] = json;
    }

    public bool TryTake(string playerName, out string json)
    {
        json = null;
        return !string.IsNullOrEmpty(playerName) && _json.Remove(playerName, out json);
    }

    public void Flush(Action<string, string> write)
    {
        foreach (var pair in _json) write(pair.Key, pair.Value);
        _json.Clear();
    }

    public void Clear() => _json.Clear();
}
