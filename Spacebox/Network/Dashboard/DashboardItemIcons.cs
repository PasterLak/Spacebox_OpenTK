using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Spacebox.Network.Dashboard;

public sealed class DashboardItemIcons
{
    private const string ItemsFolder = "Items";
    private const string BlocksFolder = "Blocks";

    private readonly string _gameSetsDirectory;
    private readonly ConcurrentDictionary<string, byte[]> _icons = new ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _texturesByMod = new ConcurrentDictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    public DashboardItemIcons(string gameSetsDirectory)
    {
        _gameSetsDirectory = gameSetsDirectory;
    }

    public byte[] Get(string fullId)
    {
        if (string.IsNullOrWhiteSpace(fullId) || !fullId.Contains(':')) return null;
        return _icons.GetOrAdd(fullId, Find);
    }

    private byte[] Find(string fullId)
    {
        int colon = fullId.IndexOf(':');
        string modId = fullId.Substring(0, colon);
        string id = fullId.Substring(colon + 1);

        if (!GameSetLocator.TryFindFolderPath(_gameSetsDirectory, modId, out var folder)) return null;

        var textures = _texturesByMod.GetOrAdd(modId, _ => ReadTextureNames(folder));
        if (!textures.TryGetValue(id, out var texture)) return null;

        string path = FindPng(Path.Combine(folder, ItemsFolder), texture) ?? FindPng(Path.Combine(folder, BlocksFolder), texture);
        return path == null ? null : File.ReadAllBytes(path);
    }

    public static Dictionary<string, string> ReadTextureNames(string gameSetFolder)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in ReadArray(Path.Combine(gameSetFolder, "blocks.json")))
        {
            string id = entry["ID"]?.GetValue<string>();
            var active = entry["Textures"]?["Active"];
            string texture = active?["Forward"]?.GetValue<string>() ?? active?["All"]?.GetValue<string>() ?? active?["Up"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(texture)) result[id] = texture;
        }

        foreach (var entry in ReadArray(Path.Combine(gameSetFolder, "items.json")))
        {
            string id = entry["ID"]?.GetValue<string>();
            string sprite = entry["Sprite"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(sprite)) result[id] = sprite;
        }

        return result;
    }

    private static IEnumerable<JsonObject> ReadArray(string path)
    {
        if (!File.Exists(path)) yield break;

        JsonArray array;
        try
        {
            array = JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonArray;
        }
        catch (JsonException)
        {
            yield break;
        }

        if (array == null) yield break;
        foreach (var node in array)
        {
            if (node is JsonObject entry) yield return entry;
        }
    }

    private static string FindPng(string directory, string name)
    {
        if (!Directory.Exists(directory)) return null;
        return Directory.EnumerateFiles(directory, Path.GetFileName(name) + ".png", SearchOption.AllDirectories).FirstOrDefault();
    }
}
