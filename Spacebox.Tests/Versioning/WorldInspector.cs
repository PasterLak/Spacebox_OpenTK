using System.Text.Json.Nodes;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Player.Respawn;
using Spacebox.Game.Versioning;

namespace Spacebox.Tests.Versioning;

public static class WorldInspector
{
    public static string Version(string folder)
    {
        return Spacebox.Game.GUI.WorldInfoFiles.TryRead(folder, out var info) ? info.GameVersion : null;
    }

    public static List<string> BlockPalettes(string folder) => Palettes(folder, true);

    public static List<string> ItemPalettes(string folder) => Palettes(folder, false);

    public static List<string> PlayerItems(string folder)
    {
        var items = new List<string>();
        foreach (var file in WorldFileLayout.LegacyPlayerFiles(folder))
        {
            var player = JsonNode.Parse(File.ReadAllText(file));
            foreach (var list in new[] { "InventorySlots", "PanelSlots", "EquipmentSlots" })
            {
                if (player?[list] is not JsonArray slots) continue;
                foreach (var slot in slots) items.Add(slot?["ItemID"]?.GetValue<string>());
            }
        }
        return items;
    }

    public static List<string> DropItems(string folder)
    {
        var items = new List<string>();
        string path = WorldFileLayout.LegacyDropsPath(folder);
        if (!File.Exists(path)) return items;

        foreach (var drop in (JsonArray)JsonNode.Parse(File.ReadAllText(path))) items.Add(drop?["ItemId"]?.GetValue<string>());
        return items;
    }

    public static List<string> EntityFiles(string folder)
    {
        var files = new List<string>();
        foreach (var file in WorldFileLayout.EntityFilePaths(folder)) files.Add(Path.GetRelativePath(folder, file).Replace(Path.DirectorySeparatorChar, '/'));
        files.Sort(StringComparer.Ordinal);
        return files;
    }

    public static List<EntityRecord> EntityIndex(string folder)
    {
        string path = EntityCatalogFile.PathOf(folder);
        return File.Exists(path) ? EntityCatalogFile.FromTag(NbtFileEditor.Read(path)) : new List<EntityRecord>();
    }

    public static List<ReconstructorRecord> Reconstructors(string folder) => ReconstructorRecordsFile.Read(folder);

    public static Dictionary<string, byte[]> Snapshot(string folder)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            files[Path.GetRelativePath(folder, file)] = File.ReadAllBytes(file);
        return files;
    }

    private static List<string> Palettes(string folder, bool blocks)
    {
        var ids = new List<string>();
        foreach (var file in WorldFileLayout.EntityFilePaths(folder))
        {
            foreach (var chunk in ChunkTagAccess.Chunks(NbtFileEditor.Read(file)))
                ids.AddRange(blocks ? ChunkTagAccess.BlockPalette(chunk) : ChunkTagAccess.ItemPalette(chunk));
        }
        return ids;
    }
}
