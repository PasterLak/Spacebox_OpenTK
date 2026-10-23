using OpenTK.Mathematics;
using Spacebox.Game.Disassembly;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using Engine;
using Engine.Audio;
using Engine.Graphics.Atlas;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Generation.Structures;
using System.Linq;
using System.Collections.Generic;

namespace Spacebox.Game
{
    public static class GameAssets
    {
        public static bool IsInitialized { get; set; } = false;
      

        public static Texture2D BlocksTexture => AtlasBlocks?.Texture;
        public static Texture2D ItemsTexture => AtlasItems?.Texture;
        public static Texture2D EmissionBlocks => AtlasBlocks?.GetLayerTexture(GameAtlasFactory.EmissionLayer);
        public static Texture2D EmissionItems => AtlasItems?.GetLayerTexture(GameAtlasFactory.EmissionLayer);
        public static Texture2D DustTexture { get; set; }

        public static LootConfig LootConfig { get; set; }

        public static Dictionary<short, BlockData> Blocks = new Dictionary<short, BlockData>();
        public static Dictionary<string, BlockData> BlocksStr { get; private set; } = new Dictionary<string, BlockData>();
        public static Dictionary<string, Item> ItemsStr { get; private set; } = new Dictionary<string, Item>();

        public static Dictionary<short, Item> Items = new Dictionary<short, Item>();
        public static Dictionary<short, ItemModel> ItemModels = new Dictionary<short, ItemModel>();
        public static Dictionary<short, Model> ItemWorldModels = new Dictionary<short, Model>();
        public static Dictionary<short, ItemMuzzle> ItemMuzzles = new Dictionary<short, ItemMuzzle>();

        public static Dictionary<short, Texture2D> ItemIcons = new Dictionary<short, Texture2D>();
        public static Dictionary<short, Texture2D> BlockDusts = new Dictionary<short, Texture2D>();
        public static Dictionary<short, AudioClip> ItemSounds = new Dictionary<short, AudioClip>();
        public static Dictionary<string, AudioClip> Sounds = new Dictionary<string, AudioClip>();

        public static Dictionary<string, Dictionary<short, Recipe>> Recipes = new Dictionary<string, Dictionary<short, Recipe>>();
        public static Dictionary<string, CraftingCategory> CraftingCategories = new Dictionary<string, CraftingCategory>();
        public static Dictionary<short, Blueprint> Blueprints = new Dictionary<short, Blueprint>();

        public static Dictionary<short, ProjectileParameters> Projectiles = new Dictionary<short, ProjectileParameters>();
        public static Dictionary<string, Mobs.MobDefinition> Mobs = new Dictionary<string, Mobs.MobDefinition>();

        public static TextureAtlas AtlasBlocks;
        public static TextureAtlas AtlasItems;

        private static short MaxBlockId = -1;
        private static short MaxItemId = -1;

        private static readonly Vector2[] DefaultUVs = new Vector2[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f)
        };

        public static Texture2D GetItemIconTexture(short itemId)
        {
            if (ItemIcons.TryGetValue(itemId, out var cached)) return cached;
            if (!Items.TryGetValue(itemId, out var item) || !item.Icon.IsValid) return null;

            var texture = AtlasItems.CreateTexture(item.Icon.Region.Name, false);
            if (texture == null) return null;

            texture.Name = item.Id_string + "_icon";
            ItemIcons.Add(itemId, texture);
            return texture;
        }

        public static bool TryGetRecipe(string type, short id, out Recipe recipe)
        {
            recipe = null;
            if (Recipes.TryGetValue(type, out var dic))
            {
                if (dic.TryGetValue(id, out Recipe rec))
                {
                    recipe = rec;
                    return true;
                }
                return false;
            }
            return false;
        }

        public static Model GetItemWorldModelById(short id)
        {
            if (!ItemWorldModels.ContainsKey(id))
                return null;
            return ItemWorldModels[id];
        }

        public static bool TryGetItemSound(short id, out AudioClip clip)
        {
            return ItemSounds.TryGetValue(id, out clip);
        }

        public static bool TryGetItemById(short id, out Item item)
        {
            return Items.TryGetValue(id, out item);
        }

        public static Item? GetItemByName(string name)
        {
            foreach (var item in Items.Values)
            {
                if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            Debug.Error("[GameAssets] GetItemByName error: Wrong name - " + name);
            return null;
        }

        public static bool HasItem(string fullId)
        {
            return ItemsStr.ContainsKey(fullId);
        }

        private static readonly Dictionary<string, string> IdAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static string CanonicalId(string idFull)
        {
            if (string.IsNullOrEmpty(idFull) || ItemsStr.ContainsKey(idFull) || BlocksStr.ContainsKey(idFull)) return idFull;
            return IdAliases.TryGetValue(idFull, out var aliased) ? aliased : idFull;
        }

        public static bool TryGetBlockByFullID(string idFull, out BlockData block)
        {
            return BlocksStr.TryGetValue(CanonicalId(idFull), out block);
        }

        private static void RegisterAlias(string fullId)
        {
            string baseNamespace = GameSetLoader.ModInfo?.ModId;
            int colon = fullId.IndexOf(':');
            if (string.IsNullOrEmpty(baseNamespace) || colon < 0) return;
            if (string.Equals(fullId.Substring(0, colon), baseNamespace, StringComparison.OrdinalIgnoreCase)) return;
            IdAliases.TryAdd(baseNamespace + ":" + fullId.Substring(colon + 1), fullId);
        }

        public static BlockData? GetBlockByFullID(string idFull)
        {
            if (BlocksStr.TryGetValue(CanonicalId(idFull), out var block))
                return block;

            Debug.Error("[GameAssets] GetBlockByFullID error: Wrong string id - " + idFull);
            return null;
        }

        public static Item? GetItemByFullID(string idFull)
        {
            if (ItemsStr.TryGetValue(CanonicalId(idFull), out var item))
                return item;

            Debug.Error("[GameAssets] GetItemByFullID error: Wrong string id - " + idFull);
            return null;
        }

        public static bool TryGetItemByFullID(string idFull, out Item item)
        {
            return ItemsStr.TryGetValue(CanonicalId(idFull), out item);
        }

        public static string GetBlockFullId(Block block)
        {
            if (Blocks.TryGetValue(block.Id, out var data))
                return data.Id_string;
            return "";
        }

        public static BlockData GetBlockDataById(short id)
        {
            if (Blocks.TryGetValue(id, out var data))
                return data;
            return Blocks[0];
        }

        public static Block CreateBlockFromId(short id)
        {
            if (Blocks.TryGetValue(id, out var data))
                return BlockFactory.CreateBlock(data);

            return new Block();
        }

        public static bool TryGetItemByBlockID(int blockID, out Item item)
        {
            return Items.TryGetValue((short)blockID, out item);
        }

        public static Vector2[] GetBlockUVsById(short id, Face face)
        {
            if (Blocks.TryGetValue(id, out var data))
                return data.GetFaceUV(face);

            return DefaultUVs;
        }

        public static Vector2[] GetBlockUVs(Block block, Face face)
        {
            if (!Blocks.TryGetValue(block.Id, out var data))
                return DefaultUVs;

            if (block is ElectricalBlock electricalBlock)
            {
                if (!electricalBlock.IsActive)
                {
                    return data.GetFaceUV(face, BlockState.Inactive) ?? DefaultUVs;
                }
            }
            return data.GetFaceUV(face, BlockState.Active) ?? DefaultUVs;
        }

        public static Direction GetBaseFrontDirection(short id)
        {
            if (Blocks.TryGetValue(id, out var data))
                return data.BaseFrontDirection;

            return Direction.Up;
        }

        public static AudioClip GetBlockAudioClipFromItemID(Item item, BlockInteractionType type)
        {
            return GetBlockAudioClipFromItemID(item.Id, type);
        }

        public static AudioClip GetBlockAudioClipFromItemID(short itemId, BlockInteractionType type)
        {
            var blockData = GetBlockDataById(itemId);
            string name = type == BlockInteractionType.Place ? blockData?.SoundPlace : blockData?.SoundDestroy;
            return name != null && Sounds.TryGetValue(name, out var clip) ? clip : null;
        }

        public static Storage CreateCreativeStorage(byte sizeX, List<Item> items)
        {
            List<Item> filtered = items.Where(i => i.Id > 1).ToList();
            byte sizeY = (byte)((filtered.Count + sizeX - 1) / sizeX);

            Storage storage = new Storage(sizeX, sizeY);

            int idx = 0;
            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    if (idx >= filtered.Count) break;
                    ItemSlot slot = storage.GetSlot(x, y);
                    slot.Item = filtered[idx];
                    slot.Count = 1;
                    idx++;
                }
            }

            return storage;
        }

        public static Storage CreateCreativeStorage(byte sizeX)
        {
            var allItems = Items.Values.ToList();
            byte sizeY = (byte)((allItems.Count + sizeX - 1) / sizeX);

            Storage storage = new Storage(sizeX, sizeY);

            int idx = 0;
            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    if (idx >= allItems.Count) break;

                    ItemSlot slot = storage.GetSlot(x, y);
                    slot.Item = allItems[idx];
                    slot.Count = 1;
                    idx++;
                }
                if (idx >= allItems.Count) break;
            }
            return storage;
        }

        public static bool TryGetProjectileByName(string name, out ProjectileParameters projectile)
        {
            foreach (var item in Projectiles.Values)
            {
                if (item.Name == name)
                {
                    projectile = item;
                    return true;
                }
            }
            projectile = new ProjectileParameters();
            return false;
        }

        public static void AddBlockString(string fullId, BlockData blockData)
        {
            BlocksStr[fullId] = blockData;
            RegisterAlias(fullId);
        }

        public static void AddItemString(string fullId, Item item)
        {
            ItemsStr[fullId] = item;
            RegisterAlias(fullId);
        }

        public static T LoadResource<T>(string path) where T : IResource, new()
        {
            if(GameSetLoader.ModInfo == null)
            {
                return Resources.Load<T>(path);
            }

            var modPath = GameSetLoader.ModInfo.ModPath;
            return ModResourceLoader.Load<T>(modPath, path);
        }

        public static void DisposeAll()
        {
            DustTexture?.Dispose();

            foreach (var texture in ItemIcons.Values)
                texture.Dispose();
            foreach (var texture in BlockDusts.Values)
                texture.Dispose();
            foreach (var c in ItemSounds.Values)
                c?.Dispose();
            foreach (var c in Sounds.Values)
                c?.Dispose();

            foreach (var itemModel in ItemModels.Values)
                itemModel.Destroy();
            foreach (var itemModel in ItemWorldModels.Values)
                itemModel.Destroy();

            AtlasBlocks?.Dispose();
            AtlasItems?.Dispose();

            Recipes.Clear();
            DisassemblyRecipes.Clear();
            LootConfig = null;
            Projectiles.Clear();
            Mobs.Clear();
            Player.Armor.ArmorLibrary.Clear();
            Blocks.Clear();
            Items.Clear();
            BlocksStr.Clear();
            ItemsStr.Clear();
            IdAliases.Clear();
            ItemModels.Clear();
            ItemWorldModels.Clear();
            ItemMuzzles.Clear();
            ItemIcons.Clear();
            BlockDusts.Clear();
            ItemSounds.Clear();
            Sounds.Clear();
            Blueprints.Clear();
            CraftingCategories.Clear();

            GameSetLoader.Unload();

            AtlasBlocks = null;
            AtlasItems = null;

            MaxBlockId = -1;
            MaxItemId = -1;
          
            IsInitialized = false;
        }

        public const int MaxBlockCount = 4096;

        public static void IncrementBlockId(BlockData blockData)
        {
            if (MaxBlockId + 1 >= MaxBlockCount)
                throw new InvalidOperationException($"Too many blocks: the GameSet and its extensions can have at most {MaxBlockCount} blocks");
            MaxBlockId++;
            blockData.Id = MaxBlockId;
        }

        public static void IncrementItemId(Item item)
        {
            MaxItemId++;
            item.Id = MaxItemId;
        }
    }
}