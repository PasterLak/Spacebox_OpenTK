using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Spacebox.Game.Resource;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Network.Dashboard;

public sealed class DashboardItemIcons
{
    private sealed class IconSource
    {
        public Dictionary<string, string> Textures;
        public GameSetAssetIndex Assets;
    }

    private readonly ModFolders _gameSetsDirectory;
    private readonly ConcurrentDictionary<string, byte[]> _icons = new ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IconSource> _sources = new ConcurrentDictionary<string, IconSource>(StringComparer.OrdinalIgnoreCase);

    public DashboardItemIcons(ModFolders gameSetsDirectory)
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

        var source = _sources.GetOrAdd(modId, CreateSource);
        if (source == null || !source.Textures.TryGetValue(id, out var texture)) return null;

        string name = Path.GetFileName(texture).ToLowerInvariant();
        string path = source.Assets.ItemTextures.TryGetValue(name, out var item) ? item : source.Assets.BlockTextures.TryGetValue(name, out var block) ? block : null;
        return path == null || !File.Exists(path) ? null : File.ReadAllBytes(path);
    }

    private IconSource CreateSource(string modId)
    {
        var loaded = GameSetLoader.Stack;
        var extensions = loaded != null && loaded.Base.Matches(modId) ? loaded.ExtensionIds : null;
        var result = GameSetStackResolver.Resolve(_gameSetsDirectory, modId, extensions);
        if (result.Stack == null) return null;

        return new IconSource
        {
            Textures = ReadTextureNames(result.Stack.ReadContent()),
            Assets = GameSetAssetIndex.Build(result.Stack.Layers)
        };
    }

    public static Dictionary<string, string> ReadTextureNames(string gameSetFolder)
    {
        return ReadTextureNames(GameSetContentReader.ReadFolder(gameSetFolder));
    }

    public static Dictionary<string, string> ReadTextureNames(GameSetContent content)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in content.Blocks)
        {
            string id = ContentJson.GetString(entry.Data, "ID");
            var active = (ContentJson.Get(entry.Data, "Textures") as JsonObject)?["Active"] as JsonObject;
            string texture = ContentJson.GetString(active, "Forward") ?? ContentJson.GetString(active, "All") ?? ContentJson.GetString(active, "Up");
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(texture)) result[id] = texture;
        }

        foreach (var entry in content.Items)
        {
            string id = ContentJson.GetString(entry.Data, "ID");
            string sprite = ContentJson.GetString(entry.Data, "Sprite");
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(sprite)) result[id] = sprite;
        }

        return result;
    }
}
