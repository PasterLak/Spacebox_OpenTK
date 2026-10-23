using System.Text.Json;
using System.Text.Json.Nodes;

namespace Spacebox.Game.Resource;

public static class ModArchiveConfig
{
    public static string? ModName(JsonNode? config) => ReadString(config, "ModName");

    public static string InstallId(JsonNode? config, string targetFolderName)
    {
        return ReadString(config, "ModId") ?? targetFolderName.ToLowerInvariant();
    }

    private static string? ReadString(JsonNode? config, string name)
    {
        if (config is not JsonObject obj) return null;

        foreach (var pair in obj)
        {
            if (!string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)) continue;
            if (pair.Value is not JsonValue value || value.GetValueKind() != JsonValueKind.String) return null;

            string? text = value.GetValue<string>()?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }

        return null;
    }
}
