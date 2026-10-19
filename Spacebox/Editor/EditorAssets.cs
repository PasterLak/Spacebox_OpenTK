using Engine.Graphics.Atlas;
using Spacebox.Game;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Editor;

public sealed class EditorAssets
{
    private readonly List<string> _itemIds = new List<string>();
    private readonly List<string> _blockTextures = new List<string>();
    private readonly List<string> _itemSprites = new List<string>();
    private readonly List<string> _sounds = new List<string>();
    private readonly List<string> _projectiles = new List<string>();
    private readonly List<string> _categories = new List<string>();
    private readonly List<string> _mobs = new List<string>();
    private readonly List<string> _lootTables = new List<string>();
    private readonly List<string> _asteroids = new List<string>();
    private readonly List<string> _biomes = new List<string>();

    public int Version { get; private set; }
    public string ModId => GameSetLoader.ModInfo?.ModId ?? "default";
    public bool IsLoaded => GameAssets.IsInitialized && GameSetLoader.ModInfo != null;

    public IReadOnlyList<string> ItemIds => _itemIds;
    public IReadOnlyList<string> BlockTextures => _blockTextures;
    public IReadOnlyList<string> ItemSprites => _itemSprites;
    public IReadOnlyList<string> Sounds => _sounds;
    public IReadOnlyList<string> Projectiles => _projectiles;
    public IReadOnlyList<string> Categories => _categories;
    public IReadOnlyList<string> Mobs => _mobs;
    public IReadOnlyList<string> LootTables => _lootTables;
    public IReadOnlyList<string> Asteroids => _asteroids;
    public IReadOnlyList<string> Biomes => _biomes;

    public void Refresh()
    {
        Version++;
        Fill(_itemIds, GameAssets.ItemsStr.Keys, ShortId);
        Fill(_blockTextures, GameAssets.AtlasBlocks?.Names, name => name);
        Fill(_itemSprites, GameAssets.AtlasItems?.Names, name => name);
        Fill(_sounds, GameAssets.Sounds.Keys, name => name);
        Fill(_categories, GameAssets.CraftingCategories.Keys, name => name);
        Fill(_mobs, GameAssets.Mobs.Keys, name => name);
        Fill(_lootTables, GameAssets.LootConfig?.LootTables.Keys, name => name);
        Fill(_asteroids, Game.Generation.World.WorldGenerator?.loadedAsteroids.Keys, name => name);
        Fill(_biomes, Game.Generation.World.WorldGenerator?.loadedBiomes.Keys, name => name);

        _projectiles.Clear();
        foreach (var projectile in GameAssets.Projectiles.Values) _projectiles.Add(projectile.Name);
        _projectiles.Sort(StringComparer.OrdinalIgnoreCase);
    }

    public static string ShortId(string fullId)
    {
        if (string.IsNullOrEmpty(fullId)) return "";
        int colon = fullId.IndexOf(':');
        return colon < 0 ? fullId : fullId.Substring(colon + 1);
    }

    public bool TryGetItem(string id, out Item item)
    {
        item = null;
        if (string.IsNullOrWhiteSpace(id)) return false;
        if (id.Contains(':')) return GameAssets.ItemsStr.TryGetValue(id, out item);
        return GameAssets.ItemsStr.TryGetValue(GameSetLoader.CombineId(ModId, id), out item) || GameAssets.ItemsStr.TryGetValue("default:" + id, out item);
    }

    public bool TryGetBlock(string id, out BlockData block)
    {
        block = null;
        if (string.IsNullOrWhiteSpace(id)) return false;
        if (id.Contains(':')) return GameAssets.BlocksStr.TryGetValue(id, out block);
        return GameAssets.BlocksStr.TryGetValue(GameSetLoader.CombineId(ModId, id), out block) || GameAssets.BlocksStr.TryGetValue("default:" + id, out block);
    }

    public bool TryGetProjectile(string name, out ProjectileParameters projectile)
    {
        return GameAssets.TryGetProjectileByName((name ?? "").ToLowerInvariant(), out projectile);
    }

    public AtlasSprite ItemIcon(string id)
    {
        return TryGetItem(id, out var item) ? item.Icon : AtlasSprite.None;
    }

    public AtlasSprite BlockTexture(string name)
    {
        return Sprite(GameAssets.AtlasBlocks, name);
    }

    public AtlasSprite ItemSprite(string name)
    {
        return Sprite(GameAssets.AtlasItems, name);
    }

    private static AtlasSprite Sprite(TextureAtlas atlas, string name)
    {
        if (atlas == null || string.IsNullOrWhiteSpace(name)) return AtlasSprite.None;
        var key = name.ToLowerInvariant();
        return atlas.Contains(key) ? atlas.GetSprite(key) : AtlasSprite.None;
    }

    private static void Fill(List<string> target, IEnumerable<string> source, Func<string, string> map)
    {
        target.Clear();
        if (source == null) return;

        foreach (var value in source)
        {
            var mapped = map(value);
            if (!string.IsNullOrEmpty(mapped) && !target.Contains(mapped)) target.Add(mapped);
        }
        target.Sort(StringComparer.OrdinalIgnoreCase);
    }
}
