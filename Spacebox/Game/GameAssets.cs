using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using Engine;
using Engine.Audio;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Game
{
    public static class GameAssets
    {
        public static bool IsInitialized { get; set; } = false;
        public static string ModId { get; private set; } = "";

        public static Texture2D BlocksTexture { get; set; }
        public static Texture2D ItemsTexture { get; set; }
        public static Texture2D EmissionBlocks { get; set; }
        public static Texture2D EmissionItems { get; set; }
        public static Texture2D DustTexture { get; set; }

        
        public static LootConfig LootConfig { get; set; }

        public static Dictionary<short, BlockData> Blocks = new Dictionary<short, BlockData>();

        public static Dictionary<string, BlockData> BlocksStr { get; private set; } = new Dictionary<string, BlockData>();
        public static Dictionary<string, Item> ItemsStr { get; private set; } = new Dictionary<string, Item>();

        public static Dictionary<short, Item> Items = new Dictionary<short, Item>();
        public static Dictionary<short, ItemModel> ItemModels = new Dictionary<short, ItemModel>();

        public static Dictionary<short, Texture2D> ItemIcons = new Dictionary<short, Texture2D>();
        public static Dictionary<short, Texture2D> BlockDusts = new Dictionary<short, Texture2D>();
        public static Dictionary<short, AudioClip> ItemSounds = new Dictionary<short, AudioClip>();
        public static Dictionary<string, AudioClip> Sounds = new Dictionary<string, AudioClip>();

        public static Dictionary<string, Dictionary<short, Recipe>> Recipes = new Dictionary<string, Dictionary<short, Recipe>>();
        public static Dictionary<string, CraftingCategory> CraftingCategories = new Dictionary<string, CraftingCategory>();
        public static Dictionary<short, Blueprint> Blueprints = new Dictionary<short, Blueprint>();

        public static Dictionary<short, ProjectileParameters> Projectiles = new Dictionary<short, ProjectileParameters>();

        public static AtlasTexture AtlasBlocks;
        public static AtlasTexture AtlasItems;

        private static short MaxBlockId = -1;
        private static short MaxItemId = -1;

        public static bool TryGetRecipe(string type, short id, out Recipe recipe)
        {
            recipe = null;
            if (Recipes.TryGetValue(type.ToLower(), out var dic))
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

        public static bool TryGetItemSound(short id, out AudioClip clip)
        {
            clip = null;
            if (ItemSounds.ContainsKey(id))
            {
                clip = ItemSounds[id];
                return true;
            }
            return false;
        }

        public static Item? GetItemByName(string name)
        {
            foreach (var item in Items.Values)
            {
                if (item.Name.ToLower() == name.ToLower())
                    return item;
            }
            Debug.Error("[GameAssets] GetItemByName error: Wrong name - " + name);
            return null;
        }

        public static bool HasItem(string fullId)
        {
            return ItemsStr.ContainsKey(fullId);
        }

        public static Item? GetItemByFullID(string idFull)
        {
            if (ItemsStr.ContainsKey(idFull))
                return ItemsStr[idFull];
            Debug.Error("[GameAssets] GetItemByFullID error: Wrong string id - " + idFull);
            return null;
        }

        public static bool TryGetItemByFullID(string idFull, out Item item)
        {
            item = null;
            if (ItemsStr.ContainsKey(idFull))
            {
                item = ItemsStr[idFull];
                return true;
            }
            return false;
        }


        public static BlockData GetBlockDataById(short id)
        {
            if (!Blocks.ContainsKey(id))
                return Blocks[0];
            return Blocks[id];
        }

        public static Block CreateBlockFromId(short id)
        {
            if (!Blocks.ContainsKey(id))
                return new Block();
            BlockData data = Blocks[id];
            return BlockFactory.CreateBlock(data);
        }

        public static bool TryGetItemByBlockID(int blockID, out Item item)
        {
            item = null;
            if (Items.ContainsKey((short)blockID))
            {
                item = Items[(short)blockID];
                return true;
            }
            return false;
        }

        public static Vector2[] GetBlockUVsById(short id, Face face)
        {
            if (!Blocks.ContainsKey(id))
                return new Vector2[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
   
            return Blocks[id].GetFaceUV(face);

        }

        public static Direction GetBaseFrontDirection(short id)
        {
            if (!Blocks.ContainsKey(id)) return Direction.Up;

            return Blocks[id].BaseFrontDirection;
        }


        public static AudioClip GetBlockAudioClipFromItemID(Item item, BlockInteractionType type)
        {
            return GetBlockAudioClipFromItemID(item.Id, type);
        }
        public static AudioClip GetBlockAudioClipFromItemID(short itemId, BlockInteractionType type)
        {
            var blockData = GetBlockDataById(itemId);
            if (type == BlockInteractionType.Place)
                return Sounds[blockData.SoundPlace];
            else
                return Sounds[blockData.SoundDestroy];
        }
        public static Storage CreateCreativeStorage(byte sizeX, List<Item> items)
        {
            List<Item> filtered = items.Where(i => i.Id != 0).ToList();
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
            byte sizeY = (byte)(Items.Count / sizeX);
            byte rest = (byte)(Items.Count % sizeX);
            if (rest > 0)
                sizeY++;
            Storage storage = new Storage(sizeX, sizeY);
            List<short> itemsIds = Items.Keys.ToList();
            short i = 1;
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    if (i >= itemsIds.Count) break;
                    if (!itemsIds.Contains(i))
                    {
                        i++;
                        continue;
                    }
                    else
                    {
                        ItemSlot slot = storage.GetSlot(x, y);
                        slot.Item = Items[itemsIds[i]];
                        slot.Count = 1;
                        i++;
                    }
                }
                if (i >= itemsIds.Count) break;
            }
            return storage;
        }

        public static bool TryGetProjectileByName(string name, out ProjectileParameters projectile)
        {
            if (Projectiles.Count == 0)
            {
                projectile = new ProjectileParameters();
                return false;
            }
            foreach (var item in Projectiles)
            {
                if (item.Value.Name == name)
                {
                    projectile = item.Value;
                    return true;
                }
            }
            projectile = new ProjectileParameters();
            return false;
        }

        public static void AddBlockString(string fullId, BlockData blockData)
        {
            BlocksStr[fullId] = blockData;
        }
        public static void AddItemString(string fullId, Item item)
        {
            ItemsStr[fullId] = item;
        }

        public static void DisposeAll()
        {
            BlocksTexture?.Dispose();
            ItemsTexture?.Dispose();
            EmissionBlocks?.Dispose();
            DustTexture?.Dispose();
            foreach (var texture in ItemIcons.Values)
                texture.Dispose();
            foreach (var texture in BlockDusts.Values)
                texture.Dispose();
            foreach (var c in ItemSounds.Values)
                c?.Dispose();
            foreach (var c in Sounds.Values)
                c?.Dispose();
            Recipes.Clear();
            LootConfig = null;
            Projectiles.Clear();
            Blocks.Clear();
            Items.Clear();
            foreach (var itemModel in ItemModels.Values)
                itemModel.Dispose();
            ItemModels.Clear();
            ItemIcons.Clear();
            BlockDusts.Clear();
            AtlasBlocks.Dispose();
            AtlasBlocks = null;
            AtlasItems.Dispose();
            CraftingCategories.Clear();
            AtlasItems = null;
            MaxBlockId = -1;
            MaxItemId = -1;
            ModId = "";

            IsInitialized = false;
        }

        public static void IncrementBlockId(BlockData blockData)
        {
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
