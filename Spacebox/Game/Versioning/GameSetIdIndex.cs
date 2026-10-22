using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.Versioning;

public sealed class GameSetIdIndex
{
    private const string DefaultFolder = "Default";

    private static readonly string[] BuiltInBlocks = { "air", "void" };

    private readonly HashSet<string> _blocks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _items = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public string ModId { get; }
    public int BlockCount => _blocks.Count;
    public int ItemCount => _items.Count;

    private GameSetIdIndex(string modId)
    {
        ModId = modId;
    }

    public static GameSetIdIndex Load(string gameSetsDirectory, string modId, IReadOnlyList<string> extensions = null)
    {
        string id = string.IsNullOrWhiteSpace(modId) ? "default" : modId;
        var result = GameSetStackResolver.Resolve(gameSetsDirectory, id, extensions);
        if (result.Stack == null)
            result = GameSetStackResolver.Resolve(gameSetsDirectory, null, extensions, DefaultFolder);

        var index = new GameSetIdIndex(id);
        if (result.Stack != null) index.AddContent(result.Stack.ReadContent());
        index.AddBuiltIns();
        return index;
    }

    public static GameSetIdIndex FromContent(GameSetContent content, string modId)
    {
        var index = new GameSetIdIndex(modId);
        index.AddContent(content);
        index.AddBuiltIns();
        return index;
    }

    private void AddContent(GameSetContent content)
    {
        foreach (var entry in content.Blocks) AddId(entry, _blocks);
        foreach (var entry in content.Items) AddId(entry, _items);
    }

    private void AddBuiltIns()
    {
        foreach (var id in BuiltInBlocks) _blocks.Add(FullId(id));
        foreach (var id in _blocks) _items.Add(id);
    }

    private void AddId(ContentEntry entry, HashSet<string> target)
    {
        string id = ContentJson.GetString(entry.Data, "ID");
        if (!string.IsNullOrWhiteSpace(id)) target.Add(FullId(id));
    }

    public bool IsBlock(string fullId) => _blocks.Contains(fullId ?? "");

    public bool IsItem(string fullId) => _items.Contains(fullId ?? "");

    public string FullId(string id)
    {
        return id.Contains(':') ? id : ModId + ":" + id;
    }
}
