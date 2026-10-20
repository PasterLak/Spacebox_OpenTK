using System.Text.Json;
using System.Text.Json.Nodes;
using SharpNBT;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.GUI;

namespace Spacebox.Game.Versioning;

public sealed class WorldUpgradeContext
{
    public const string Air = "default:air";
    public const string RemovedItem = "";

    private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions { WriteIndented = true };
    private static readonly JsonDocumentOptions ReadOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
    private static readonly string[] PlayerSlotLists = { "InventorySlots", "PanelSlots", "EquipmentSlots" };
    private const string PlayerItemKey = "ItemID";
    private const string DropItemKey = "ItemId";

    private readonly List<string> _log = new List<string>();
    private readonly Dictionary<string, int> _counters = new Dictionary<string, int>(StringComparer.Ordinal);

    public WorldUpgradeContext(WorldInfo info, string worldFolder)
    {
        Info = info;
        WorldFolder = worldFolder;
    }

    public WorldInfo Info { get; }
    public string WorldFolder { get; }
    public bool HasFolder => !string.IsNullOrEmpty(WorldFolder) && Directory.Exists(WorldFolder);
    public IReadOnlyList<string> Log => _log;
    public IReadOnlyDictionary<string, int> Counters => _counters;

    public void Write(string message)
    {
        _log.Add(message);
    }

    public void Count(string what, int amount = 1)
    {
        if (amount <= 0) return;
        _counters[what] = (_counters.TryGetValue(what, out int value) ? value : 0) + amount;
    }

    public string PathOf(string relativePath)
    {
        RequireFolder();
        return Path.Combine(WorldFolder, relativePath);
    }

    public IEnumerable<string> Files(string searchPattern, bool recursive = true)
    {
        if (!HasFolder) return Array.Empty<string>();
        return Directory.GetFiles(WorldFolder, searchPattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
    }

    public IEnumerable<string> PlayerFiles() => HasFolder ? WorldFileLayout.PlayerFiles(WorldFolder) : Array.Empty<string>();

    public void RenameBlock(string from, string to) => RemapBlocks(new IdRemap().Rename(from, to));

    public void RemoveBlock(string id, string replacement = Air) => RemapBlocks(new IdRemap().Remove(id, replacement));

    public void RenameItem(string from, string to) => RemapItems(new IdRemap().Rename(from, to));

    public void RemoveItem(string id, string replacement = null) => RemapItems(new IdRemap().Remove(id, replacement ?? RemovedItem));

    public void RemapBlocks(IdRemap blocks)
    {
        int before = blocks.Changes;
        EditChunks(chunk => ChunkTagAccess.RemapPalette(chunk, NBTKey.CHUNK.palette_blocks, blocks));
        Count("block palette entries", blocks.Changes - before);
        RemapItems(blocks.WithRemovalsAs(RemovedItem));
    }

    public void RemapItems(IdRemap items)
    {
        int before = items.Changes;
        EditChunks(chunk => ChunkTagAccess.RemapPalette(chunk, NBTKey.CHUNK.palette_items, items));
        Count("item palette entries", items.Changes - before);

        before = items.Changes;
        EditPlayers(player => RemapPlayerSlots(player, items));
        Count("player slots", items.Changes - before);

        before = items.Changes;
        EditDrops(drops => RemapDrops(drops, items));
        Count("drops", items.Changes - before);
    }

    public bool EditJson(string path, Func<JsonNode, bool> edit)
    {
        if (!File.Exists(path)) return false;

        var node = JsonNode.Parse(File.ReadAllText(path), documentOptions: ReadOptions);
        if (node == null || !edit(node)) return false;

        File.WriteAllText(path, node.ToJsonString(WriteOptions));
        return true;
    }

    public int EditJsonFiles(IEnumerable<string> paths, Func<JsonNode, bool> edit)
    {
        int changed = 0;
        foreach (var path in paths)
        {
            if (EditJson(path, edit)) changed++;
        }
        return changed;
    }

    public int EditPlayers(Func<JsonNode, bool> edit)
    {
        int changed = EditJsonFiles(PlayerFiles(), edit);
        Count("player files", changed);
        return changed;
    }

    public bool EditDrops(Func<JsonArray, bool> edit)
    {
        if (!HasFolder) return false;
        return EditJson(WorldFileLayout.DropsPath(WorldFolder), node => node is JsonArray drops && edit(drops));
    }

    public int EditSectors(Func<CompoundTag, bool> edit)
    {
        return EditNbtFiles(HasFolder ? WorldFileLayout.SectorFiles(WorldFolder) : Array.Empty<string>(), edit, "sector files");
    }

    public int EditEntities(Func<CompoundTag, bool> edit)
    {
        return EditNbtFiles(HasFolder ? WorldFileLayout.EntityFilePaths(WorldFolder) : Array.Empty<string>(), edit, "entity files");
    }

    public int EditChunks(Func<CompoundTag, bool> edit)
    {
        int chunks = 0;
        EditEntities(entity =>
        {
            bool changed = false;
            foreach (var chunk in ChunkTagAccess.Chunks(entity))
            {
                if (!edit(chunk)) continue;
                changed = true;
                chunks++;
            }
            return changed;
        });
        Count("chunks", chunks);
        return chunks;
    }

    public string Summary()
    {
        if (_counters.Count == 0) return "no file changes";

        var parts = new List<string>(_counters.Count);
        foreach (var pair in _counters) parts.Add($"{pair.Value} {pair.Key}");
        return "changed " + string.Join(", ", parts);
    }

    private int EditNbtFiles(IEnumerable<string> files, Func<CompoundTag, bool> edit, string what)
    {
        int changed = 0;
        foreach (var file in files)
        {
            if (NbtFileEditor.Edit(file, edit)) changed++;
        }
        Count(what, changed);
        return changed;
    }

    private static bool RemapPlayerSlots(JsonNode player, IdRemap items)
    {
        bool changed = false;
        foreach (var listName in PlayerSlotLists)
        {
            if (player[listName] is not JsonArray slots) continue;

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i] is not JsonObject slot || !items.TryMap(slot[PlayerItemKey]?.GetValue<string>(), out var mapped)) continue;

                if (string.IsNullOrEmpty(mapped)) slots.RemoveAt(i);
                else slot[PlayerItemKey] = mapped;
                changed = true;
            }
        }
        return changed;
    }

    private static bool RemapDrops(JsonArray drops, IdRemap items)
    {
        bool changed = false;
        for (int i = drops.Count - 1; i >= 0; i--)
        {
            if (drops[i] is not JsonObject drop || !items.TryMap(drop[DropItemKey]?.GetValue<string>(), out var mapped)) continue;

            if (string.IsNullOrEmpty(mapped)) drops.RemoveAt(i);
            else drop[DropItemKey] = mapped;
            changed = true;
        }
        return changed;
    }

    private void RequireFolder()
    {
        if (!HasFolder) throw new InvalidOperationException("This upgrade step needs the world folder, but it was not found");
    }
}
