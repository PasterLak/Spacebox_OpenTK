using System.Text.Json;
using System.Text.Json.Nodes;
using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Versioning;

namespace Spacebox.Tests.Versioning;

public sealed class WorldFixtureBuilder
{
    private const int ChunkVolume = 32 * 32 * 32;
    public const string EntityFolderVersion = "0.1.5";

    private sealed class ChunkSpec
    {
        public string[] Blocks;
        public string[] Items;
        public int[] Cells;
        public List<(int Flat, int Item, int Count)> Storages = new List<(int, int, int)>();
    }

    private readonly string _folder;
    private readonly List<(string Name, List<ChunkSpec> Chunks)> _entities = new List<(string, List<ChunkSpec>)>();
    private readonly Dictionary<string, JsonObject> _players = new Dictionary<string, JsonObject>();
    private readonly JsonArray _drops = new JsonArray();
    private readonly Dictionary<string, long> _entityIds = new Dictionary<string, long>();
    private string _version = Engine.Application.Version;
    private string _modId = "default";
    private string _name = "Fixture";
    private bool _legacyEntityLayout;

    public WorldFixtureBuilder(string folder)
    {
        _folder = folder;
    }

    public static string SectorFolderName => SpaceMath.Sector.IndexToFolderName(new Vector3i(0, 0, 0));

    public WorldFixtureBuilder Version(string version)
    {
        _version = version;
        return this;
    }

    public WorldFixtureBuilder Mod(string modId)
    {
        _modId = modId;
        return this;
    }

    public WorldFixtureBuilder LegacyEntityLayout()
    {
        _legacyEntityLayout = true;
        return this;
    }

    public WorldFixtureBuilder Named(string name)
    {
        _name = name;
        return this;
    }

    public WorldFixtureBuilder Chunk(string[] blocks, string[] items = null, string entity = "1")
    {
        EntityChunks(entity).Add(new ChunkSpec { Blocks = blocks ?? new[] { "default:air" }, Items = items ?? Array.Empty<string>() });
        return this;
    }

    public WorldFixtureBuilder SparseChunk(string entity, string block, Vector3i[] cells, string storedItem = null, int storedCount = 0)
    {
        var spec = new ChunkSpec { Blocks = new[] { "default:air", block }, Items = storedItem != null ? new[] { storedItem } : Array.Empty<string>(), Cells = new int[ChunkVolume] };
        foreach (var cell in cells)
        {
            int flat = (cell.X * 32 + cell.Y) * 32 + cell.Z;
            spec.Cells[flat] = 1;
            if (storedItem != null) spec.Storages.Add((flat, 0, storedCount));
        }

        EntityChunks(entity).Add(spec);
        return this;
    }

    public WorldFixtureBuilder EntityId(string entity, long id)
    {
        _entityIds[entity] = id;
        return this;
    }

    private List<ChunkSpec> EntityChunks(string entity)
    {
        int index = _entities.FindIndex(e => e.Name == entity);
        if (index >= 0) return _entities[index].Chunks;

        _entities.Add((entity, new List<ChunkSpec>()));
        return _entities[^1].Chunks;
    }

    public static bool UsesEntityFolder(string version)
    {
        return System.Version.TryParse(version, out var parsed) && parsed >= System.Version.Parse(EntityFolderVersion);
    }

    public WorldFixtureBuilder Player(string item, int count = 1, string file = WorldFileLayout.LocalPlayerFile, string list = "InventorySlots")
    {
        if (!_players.TryGetValue(file, out var player))
        {
            player = new JsonObject
            {
                ["Health"] = 100,
                ["InventorySlots"] = new JsonArray(),
                ["PanelSlots"] = new JsonArray(),
                ["EquipmentSlots"] = new JsonArray()
            };
            _players[file] = player;
        }

        var slots = (JsonArray)player[list];
        slots.Add(new JsonObject { ["ItemID"] = item, ["Count"] = count, ["SlotX"] = slots.Count, ["SlotY"] = 0 });
        return this;
    }

    public WorldFixtureBuilder Drop(string item, int quantity = 1)
    {
        _drops.Add(new JsonObject { ["ItemId"] = item, ["Quantity"] = quantity, ["X"] = 1f, ["Y"] = 2f, ["Z"] = 3f });
        return this;
    }

    public string Build()
    {
        Directory.CreateDirectory(_folder);
        var options = new JsonSerializerOptions { WriteIndented = true };

        var world = new JsonObject
        {
            ["Name"] = _name,
            ["Author"] = "Tests",
            ["GameMode"] = 0,
            ["Seed"] = "1",
            ["ModId"] = _modId,
            ["GameVersion"] = _version,
            ["FolderName"] = Path.GetFileName(_folder)
        };
        File.WriteAllText(Path.Combine(_folder, WorldFileLayout.WorldFile), world.ToJsonString(options));

        foreach (var (file, player) in _players)
        {
            string path = Path.Combine(_folder, file);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, player.ToJsonString(options));
        }

        if (_drops.Count > 0) File.WriteAllText(WorldFileLayout.DropsPath(_folder), _drops.ToJsonString(options));

        string sector = Path.Combine(_folder, WorldFileLayout.SectorsFolder, SectorFolderName);
        Directory.CreateDirectory(sector);
        NbtFileEditor.Write(Path.Combine(sector, SectorFolderName + WorldFileLayout.SectorExtension), CreateSector());

        bool entityFolder = !_legacyEntityLayout && UsesEntityFolder(_version);
        var catalog = new EntityCatalog();
        for (int i = 0; i < _entities.Count; i++)
        {
            var (name, chunks) = _entities[i];
            long id = _entityIds.TryGetValue(name, out long explicitId) ? explicitId : -(i + 1);
            var entity = CreateEntity(id, name, chunks);
            if (!entityFolder)
            {
                NbtFileEditor.Write(EntityFiles.PathOf(sector, name), entity);
                continue;
            }

            EntityTagLocation.Write(entity, Vector3i.Zero, EntityPosition);
            Directory.CreateDirectory(EntityFiles.FolderOf(_folder));
            NbtFileEditor.Write(EntityFiles.PathInWorld(_folder, name), entity);
            catalog.Upsert(new EntityRecord(id, name, Vector3i.Zero, EntityPosition));
        }

        if (entityFolder) EntityCatalogFile.Write(_folder, catalog);
        return _folder;
    }

    public static readonly Vector3 EntityPosition = new Vector3(10f, 20f, 30f);

    private static CompoundTag CreateSector()
    {
        var root = new CompoundTag("Sector");
        root.Add(new IntTag(NBTKey.SECTOR.index_x, 0));
        root.Add(new IntTag(NBTKey.SECTOR.index_y, 0));
        root.Add(new IntTag(NBTKey.SECTOR.index_z, 0));
        root.Add(new LongArrayTag(NBTKey.SECTOR.suppressed_ids, Array.Empty<long>()));
        return root;
    }

    private static CompoundTag CreateEntity(long id, string name, List<ChunkSpec> chunks)
    {
        var root = new CompoundTag("SpaceEntity");
        root.Add(new LongTag(NBTKey.ENTITY.id, id));
        root.Add(new StringTag(NBTKey.ENTITY.name, name));
        root.Add(new FloatTag(NBTKey.ENTITY.local_x, EntityPosition.X));
        root.Add(new FloatTag(NBTKey.ENTITY.local_y, EntityPosition.Y));
        root.Add(new FloatTag(NBTKey.ENTITY.local_z, EntityPosition.Z));
        root.Add(new FloatTag(NBTKey.ENTITY.rotation_x, 0f));
        root.Add(new FloatTag(NBTKey.ENTITY.rotation_y, 0f));
        root.Add(new FloatTag(NBTKey.ENTITY.rotation_z, 0f));
        root.Add(new FloatTag(NBTKey.ENTITY.rotation_w, 1f));
        root.Add(new ByteTag(NBTKey.ENTITY.only_changed_chunks, 0));

        var list = new ListTag(NBTKey.ENTITY.chunks, TagType.Compound);
        for (int i = 0; i < chunks.Count; i++) list.Add(CreateChunk(chunks[i], i));
        root.Add(list);
        return root;
    }

    private static CompoundTag CreateChunk(ChunkSpec spec, int index)
    {
        var chunk = new CompoundTag("Chunk");
        chunk.Add(new ByteTag(NBTKey.CHUNK.index_x, PackingTools.SByteToByte((sbyte)index)));
        chunk.Add(new ByteTag(NBTKey.CHUNK.index_y, 0));
        chunk.Add(new ByteTag(NBTKey.CHUNK.index_z, 0));

        var blocks = new byte[ChunkVolume];
        for (int i = 0; i < blocks.Length; i++) blocks[i] = spec.Cells != null ? (byte)spec.Cells[i] : (byte)(i % spec.Blocks.Length);
        chunk.Add(new ByteArrayTag(NBTKey.CHUNK.blocks, blocks));

        chunk.Add(StringList(NBTKey.CHUNK.palette_blocks, spec.Blocks));
        chunk.Add(StringList(NBTKey.CHUNK.palette_items, spec.Items));
        chunk.Add(new IntArrayTag(NBTKey.CHUNK.directions, Array.Empty<int>()));
        chunk.Add(new IntArrayTag(NBTKey.CHUNK.rotations, Array.Empty<int>()));
        var storages = new ListTag(NBTKey.CHUNK.storages, TagType.Compound);
        foreach (var (flat, item, count) in spec.Storages)
        {
            var storage = new CompoundTag(null);
            storage.Add(new IntTag("flat_index", flat));
            storage.Add(new IntTag("slot_data", PackingTools.PackShorts((short)item, (short)count)));
            storages.Add(storage);
        }
        chunk.Add(storages);
        return chunk;
    }

    private static ListTag StringList(string name, string[] values)
    {
        var list = new ListTag(name, TagType.String);
        foreach (var value in values) list.Add(new StringTag(null, value));
        return list;
    }
}
