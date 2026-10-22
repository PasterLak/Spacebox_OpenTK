using System.Text.Json;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Game.Versioning;

public static class WorldValidator
{
    private const int MaxListedIds = 8;

    private static readonly string[] PlayerSlotLists = { "InventorySlots", "PanelSlots", "EquipmentSlots" };

    public static List<string> FindUnknownIds(string worldFolder, GameSetIdIndex ids)
    {
        var unknownBlocks = new SortedSet<string>(StringComparer.Ordinal);
        var unknownItems = new SortedSet<string>(StringComparer.Ordinal);
        var warnings = new List<string>();
        if (ids == null || string.IsNullOrEmpty(worldFolder) || !Directory.Exists(worldFolder)) return warnings;

        foreach (var file in WorldFileLayout.EntityFilePaths(worldFolder))
        {
            var entity = NbtFileEditor.Read(file);
            foreach (var chunk in ChunkTagAccess.Chunks(entity))
            {
                AddUnknown(ChunkTagAccess.ReadPalette(chunk, NBTKey.CHUNK.palette_blocks), ids.IsBlock, unknownBlocks);
                AddUnknown(ChunkTagAccess.ReadPalette(chunk, NBTKey.CHUNK.palette_items), ids.IsItem, unknownItems);
            }
        }

        foreach (var file in WorldFileLayout.LegacyPlayerFiles(worldFolder))
            AddUnknown(ReadJsonIds(file, PlayerSlotLists, "ItemID"), ids.IsItem, unknownItems);

        AddUnknown(ReadJsonIds(WorldFileLayout.LegacyDropsPath(worldFolder), null, "ItemId"), ids.IsItem, unknownItems);

        AddWarning(warnings, "blocks", unknownBlocks);
        AddWarning(warnings, "items", unknownItems);
        return warnings;
    }

    private static void AddUnknown(IEnumerable<string> found, Func<string, bool> isKnown, SortedSet<string> unknown)
    {
        foreach (var id in found)
        {
            if (!string.IsNullOrEmpty(id) && !isKnown(id)) unknown.Add(id);
        }
    }

    private static void AddWarning(List<string> warnings, string kind, SortedSet<string> unknown)
    {
        if (unknown.Count == 0) return;

        var listed = unknown.Take(MaxListedIds);
        string more = unknown.Count > MaxListedIds ? $" and {unknown.Count - MaxListedIds} more" : "";
        warnings.Add($"{unknown.Count} unknown {kind}: {string.Join(", ", listed)}{more}");
    }

    private static IEnumerable<string> ReadJsonIds(string path, string[] lists, string key)
    {
        if (!File.Exists(path)) yield break;

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        if (lists == null)
        {
            foreach (var id in IdsInArray(root, key)) yield return id;
            yield break;
        }

        if (root.ValueKind != JsonValueKind.Object) yield break;
        foreach (var list in lists)
        {
            if (!root.TryGetProperty(list, out var array)) continue;
            foreach (var id in IdsInArray(array, key)) yield return id;
        }
    }

    private static List<string> IdsInArray(JsonElement array, string key)
    {
        var ids = new List<string>();
        if (array.ValueKind != JsonValueKind.Array) return ids;

        foreach (var entry in array.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty(key, out var id) && id.ValueKind == JsonValueKind.String)
                ids.Add(id.GetString());
        }
        return ids;
    }
}
