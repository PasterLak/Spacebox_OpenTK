using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.Utils;

namespace Spacebox.Editor.Data;

public sealed class EditorJsonFile
{
    private static readonly JsonDocumentOptions DocumentOptions = new JsonDocumentOptions
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string FullPath { get; }
    public string RelativePath { get; }
    public JsonNode Root { get; set; }
    public bool IsDirty { get; private set; }
    public string Error { get; private set; }
    public int Version { get; private set; }

    private static int _stamp;

    public static int NextStamp()
    {
        return Interlocked.Increment(ref _stamp);
    }

    private EditorJsonFile(string fullPath, string relativePath)
    {
        FullPath = fullPath;
        RelativePath = relativePath.Replace('\\', '/');
    }

    public static EditorJsonFile Load(string fullPath, string rootFolder)
    {
        var file = new EditorJsonFile(fullPath, Path.GetRelativePath(rootFolder, fullPath));
        file.Reload();
        return file;
    }

    public static EditorJsonFile Create(string fullPath, string rootFolder, JsonNode root)
    {
        var file = new EditorJsonFile(fullPath, Path.GetRelativePath(rootFolder, fullPath)) { Root = root };
        file.MarkDirty();
        return file;
    }

    public static JsonNode Parse(string text)
    {
        try
        {
            return JsonNode.Parse(text, null, DocumentOptions);
        }
        catch (JsonException)
        {
            return JsonNode.Parse(JsonFixer.FixJson(text), null, DocumentOptions);
        }
    }

    public static string Serialize(JsonNode node)
    {
        return node?.ToJsonString(WriteOptions) ?? "null";
    }

    public void Reload()
    {
        try
        {
            Root = File.Exists(FullPath) ? Parse(File.ReadAllText(FullPath)) : null;
            Error = null;
        }
        catch (Exception ex)
        {
            Root = null;
            Error = ex.Message;
        }

        IsDirty = false;
        Version = NextStamp();
    }

    public void MarkDirty()
    {
        IsDirty = true;
        Version = NextStamp();
    }

    public void Save()
    {
        if (Root == null) return;

        var directory = Path.GetDirectoryName(FullPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(FullPath, Serialize(Root));
        IsDirty = false;
    }
}
