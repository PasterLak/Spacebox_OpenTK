using System.Text.Json;

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

    public static GameSetIdIndex Load(string gameSetsDirectory, string modId)
    {
        string defaultPath = Path.Combine(gameSetsDirectory, DefaultFolder);
        if (!Spacebox.Network.GameSetLocator.TryFindFolderPath(gameSetsDirectory, modId, out var modPath)) modPath = defaultPath;
        return FromFolders(modPath, defaultPath, string.IsNullOrWhiteSpace(modId) ? "default" : modId);
    }

    public static GameSetIdIndex FromFolders(string modPath, string defaultPath, string modId)
    {
        var index = new GameSetIdIndex(modId);
        index.AddIds(PickFile(modPath, defaultPath, "blocks.json"), index._blocks);
        index.AddIds(PickFile(modPath, defaultPath, "items.json"), index._items);

        foreach (var id in BuiltInBlocks) index._blocks.Add(index.FullId(id));
        foreach (var id in index._blocks) index._items.Add(id);
        return index;
    }

    public bool IsBlock(string fullId) => _blocks.Contains(fullId ?? "");

    public bool IsItem(string fullId) => _items.Contains(fullId ?? "");

    public string FullId(string id)
    {
        return id.Contains(':') ? id : ModId + ":" + id;
    }

    private void AddIds(string path, HashSet<string> target)
    {
        if (path == null) return;

        using var document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        Collect(document.RootElement, target);
    }

    private void Collect(JsonElement element, HashSet<string> target)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray()) Collect(item, target);
            return;
        }

        if (element.ValueKind != JsonValueKind.Object) return;

        if (element.TryGetProperty("ID", out var id) && id.ValueKind == JsonValueKind.String)
        {
            target.Add(FullId(id.GetString()));
            return;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array) Collect(property.Value, target);
        }
    }

    private static string PickFile(string modPath, string defaultPath, string name)
    {
        string own = Path.Combine(modPath ?? "", name);
        if (File.Exists(own)) return own;

        string fallback = Path.Combine(defaultPath ?? "", name);
        return File.Exists(fallback) ? fallback : null;
    }
}
