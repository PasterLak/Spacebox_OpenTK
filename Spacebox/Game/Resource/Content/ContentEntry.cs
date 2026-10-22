using System.Text.Json.Nodes;

namespace Spacebox.Game.Resource.Content;

public sealed class ContentEntry
{
    public string Key { get; }
    public JsonObject Data { get; }
    public ContentLayer Layer { get; }
    public string Source { get; }
    public IReadOnlyList<string> SearchDirectories { get; }

    public ContentEntry(string key, JsonObject data, ContentLayer layer, string source, IReadOnlyList<string> searchDirectories)
    {
        Key = key ?? "";
        Data = data;
        Layer = layer;
        Source = source ?? "";
        SearchDirectories = searchDirectories ?? Array.Empty<string>();
    }

    public static ContentEntry Create(string key, JsonObject data, ContentLayer layer, string source, string entryDirectory)
    {
        var directories = new List<string>();
        if (!string.IsNullOrEmpty(entryDirectory)) directories.Add(entryDirectory);
        if (!string.IsNullOrEmpty(layer.Root) && !directories.Contains(layer.Root)) directories.Add(layer.Root);
        return new ContentEntry(key, data, layer, source, directories);
    }

    public ContentEntry PatchedBy(ContentEntry patch)
    {
        var directories = new List<string>(patch.SearchDirectories);
        foreach (var directory in SearchDirectories)
        {
            if (!directories.Contains(directory)) directories.Add(directory);
        }
        return new ContentEntry(Key, ContentJson.Merge(Data, patch.Data), patch.Layer, patch.Source, directories);
    }

    public string ResolveFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return "";

        foreach (var directory in SearchDirectories)
        {
            string path = Path.Combine(directory, relativePath);
            if (File.Exists(path)) return path;
        }
        return "";
    }
}
