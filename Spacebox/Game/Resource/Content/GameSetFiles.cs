using System.Text.Json;
using System.Text.Json.Nodes;

namespace Spacebox.Game.Resource.Content;

public sealed class LayeredFile
{
    public LayeredFile(string key, JsonNode data, ContentLayer layer, string source, IReadOnlyList<string> roots)
    {
        Key = key;
        Data = data;
        Layer = layer;
        Source = source;
        Roots = roots;
    }

    public string Key { get; }
    public string Name => Path.GetFileName(Key);
    public JsonNode Data { get; }
    public ContentLayer Layer { get; }
    public string Source { get; }
    public IReadOnlyList<string> Roots { get; }

    public string ResolvePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath)) return relativePath;
        foreach (var root in Roots)
        {
            string path = Path.Combine(root, relativePath);
            if (File.Exists(path)) return path;
        }
        return relativePath;
    }

    public LayeredFile PatchedBy(LayeredFile patch)
    {
        var roots = new List<string>(patch.Roots);
        foreach (var root in Roots)
            if (!roots.Contains(root)) roots.Add(root);
        var data = Data is JsonObject target && patch.Data is JsonObject changes ? ContentJson.Merge(target, changes) : patch.Data?.DeepClone();
        return new LayeredFile(Key, data, patch.Layer, patch.Source, roots);
    }
}

public sealed class GameSetFiles
{
    private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly IReadOnlyList<ContentLayer> _baseCandidates;
    private readonly IReadOnlyList<ContentLayer> _extensions;
    private readonly List<string> _warnings = new List<string>();

    public GameSetFiles(IReadOnlyList<ContentLayer> baseCandidates, IReadOnlyList<ContentLayer> extensions)
    {
        _baseCandidates = baseCandidates ?? Array.Empty<ContentLayer>();
        _extensions = extensions ?? Array.Empty<ContentLayer>();
    }

    public static GameSetFiles For(GameSetStack stack)
    {
        var candidates = new List<ContentLayer> { stack.Layers[0] };
        foreach (var fallback in stack.Fallbacks) candidates.Add(fallback.ToLayer());
        var extensions = new List<ContentLayer>();
        for (int i = 1; i < stack.Layers.Count; i++) extensions.Add(stack.Layers[i]);
        return new GameSetFiles(candidates, extensions);
    }

    public IReadOnlyList<string> Warnings => _warnings;
    public string BaseRoot => _baseCandidates.Count > 0 ? _baseCandidates[0].Root : "";

    public IReadOnlyList<LayeredFile> Folder(string relativeFolder, bool recursive = true)
    {
        var order = new List<string>();
        var files = new Dictionary<string, LayeredFile>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in _baseCandidates)
        {
            if (!Directory.Exists(candidate.PathOf(relativeFolder))) continue;
            AddFolder(candidate, relativeFolder, recursive, order, files);
            break;
        }

        foreach (var extension in _extensions) AddFolder(extension, relativeFolder, recursive, order, files);

        var result = new List<LayeredFile>(order.Count);
        foreach (var key in order)
            if (files.TryGetValue(key, out var file)) result.Add(file);
        return result;
    }

    public LayeredFile File(string relativePath, IReadOnlyDictionary<string, string> keyedArrays = null)
    {
        LayeredFile result = null;
        foreach (var candidate in _baseCandidates)
        {
            string path = candidate.PathOf(relativePath);
            if (!System.IO.File.Exists(path)) continue;
            result = Read(candidate, path, relativePath);
            break;
        }

        foreach (var extension in _extensions)
        {
            string path = extension.PathOf(relativePath);
            if (!System.IO.File.Exists(path)) continue;
            var patch = Read(extension, path, relativePath);
            if (patch.Data == null) continue;
            if (ContentJson.IsRemoval(patch.Data)) result = null;
            else result = result == null ? patch : MergeKeyedArrays(result, patch, keyedArrays);
        }
        return result;
    }

    public static T Deserialize<T>(JsonNode node) where T : class
    {
        return node?.Deserialize<T>(ReadOptions);
    }

    private void AddFolder(ContentLayer layer, string relativeFolder, bool recursive, List<string> order, Dictionary<string, LayeredFile> files)
    {
        string folder = layer.PathOf(relativeFolder);
        if (!Directory.Exists(folder)) return;

        var paths = Directory.GetFiles(folder, "*.json", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        Array.Sort(paths, StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            if (GameSetFormat.IsIgnoredPath(folder, path)) continue;
            string key = Path.ChangeExtension(Path.GetRelativePath(folder, path), null).Replace('\\', '/');
            var file = Read(layer, path, key);
            if (file.Data == null) continue;

            if (ContentJson.IsRemoval(file.Data))
            {
                if (!files.Remove(key)) _warnings.Add($"'{relativeFolder}/{key}' cannot be removed by '{layer.Id}': it does not exist");
                continue;
            }

            if (files.TryGetValue(key, out var existing)) files[key] = existing.PatchedBy(file);
            else
            {
                files[key] = file;
                order.Add(key);
            }
        }
    }

    private LayeredFile Read(ContentLayer layer, string path, string key)
    {
        var data = ContentJson.ParseFile(path, _warnings);
        return new LayeredFile(key, data, layer, path, new[] { layer.Root });
    }

    private static LayeredFile MergeKeyedArrays(LayeredFile target, LayeredFile patch, IReadOnlyDictionary<string, string> keyedArrays)
    {
        var merged = target.PatchedBy(patch);
        if (keyedArrays == null || target.Data is not JsonObject before || patch.Data is not JsonObject changes || merged.Data is not JsonObject result) return merged;

        foreach (var pair in keyedArrays)
        {
            if (ContentJson.Get(before, pair.Key) is not JsonArray baseArray || ContentJson.Get(changes, pair.Key) is not JsonArray patchArray) continue;
            ContentJson.Set(result, pair.Key, MergeArray(baseArray, patchArray, pair.Value));
        }
        return merged;
    }

    private static JsonArray MergeArray(JsonArray baseArray, JsonArray patchArray, string idKey)
    {
        var entries = new List<JsonObject>();
        foreach (var element in baseArray)
            if (element is JsonObject obj) entries.Add((JsonObject)obj.DeepClone());

        foreach (var element in patchArray)
        {
            if (element is not JsonObject change) continue;
            string id = ContentJson.GetString(change, idKey);
            int index = entries.FindIndex(e => string.Equals(ContentJson.GetString(e, idKey), id, StringComparison.OrdinalIgnoreCase));
            if (ContentJson.IsRemoval(change))
            {
                if (index >= 0) entries.RemoveAt(index);
                continue;
            }
            if (index >= 0) entries[index] = ContentJson.Merge(entries[index], change);
            else entries.Add((JsonObject)change.DeepClone());
        }

        var result = new JsonArray();
        foreach (var entry in entries) result.Add(entry);
        return result;
    }
}
