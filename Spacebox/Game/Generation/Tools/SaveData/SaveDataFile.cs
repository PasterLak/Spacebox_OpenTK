using System.Text.Json;
using System.Text.Json.Nodes;
using Engine;
using Engine.Utils;
using SharpNBT;

namespace Spacebox.Game.Generation.Tools.SaveData;

public static class SaveDataFile
{
    private static readonly JsonSerializerOptions Compact = new JsonSerializerOptions { WriteIndented = false };

    public static bool Exists(string path, string legacyJsonPath = null)
    {
        return File.Exists(path) || (legacyJsonPath != null && File.Exists(legacyJsonPath));
    }

    public static string ReadJson(string path, string legacyJsonPath = null)
    {
        if (File.Exists(path))
        {
            try
            {
                return JsonNbtConverter.ToJson(ReadNbt(path))?.ToJsonString(Compact);
            }
            catch (Exception ex)
            {
                Debug.Error($"[Save] Cannot read {path}: {ex.Message}");
                return null;
            }
        }

        return legacyJsonPath != null && File.Exists(legacyJsonPath) ? File.ReadAllText(legacyJsonPath) : null;
    }

    public static T Read<T>(string path, string legacyJsonPath = null, JsonSerializerOptions options = null)
    {
        string json = ReadJson(path, legacyJsonPath);
        return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, options);
    }

    public static void WriteJson(string path, string json, string legacyJsonPath = null)
    {
        var node = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        WriteNode(path, node, legacyJsonPath);
    }

    public static void Write<T>(string path, T value, string legacyJsonPath = null, JsonSerializerOptions options = null)
    {
        WriteNode(path, JsonSerializer.SerializeToNode(value, options), legacyJsonPath);
    }

    public static void Delete(string path, string legacyJsonPath = null)
    {
        if (File.Exists(path)) File.Delete(path);
        if (legacyJsonPath != null && File.Exists(legacyJsonPath)) File.Delete(legacyJsonPath);
    }

    private static CompoundTag ReadNbt(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Stream stream = new MemoryStream(bytes);
        if (bytes.Length > 1 && bytes[0] == 0x1F && bytes[1] == 0x8B) stream = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
        using var reader = new TagReader(stream, FormatOptions.Java, false);
        return reader.ReadTag<CompoundTag>(true);
    }

    private static void WriteNode(string path, JsonNode node, string legacyJsonPath)
    {
        string folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        var tag = JsonNbtConverter.ToNbt(node);
        AtomicFile.Write(path, temp => NbtFile.Write(temp, tag, FormatOptions.Java, CompressionType.GZip));
        if (legacyJsonPath != null && File.Exists(legacyJsonPath)) File.Delete(legacyJsonPath);
    }
}
