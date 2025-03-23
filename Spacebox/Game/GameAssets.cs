using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using Engine;
using Engine.Audio;

namespace Spacebox.Game
{
    public static class GameAssets
    {
        public static bool IsInitialized { get; set; } = false;
        public static string ModId { get; private set; } = "";

        public static Texture2D BlocksTexture { get; set; }
        public static Texture2D ItemsTexture { get; set; }
        public static Texture2D LightAtlas { get; set; }
        public static Texture2D DustTexture { get; set; }

        public static Dictionary<short, BlockData> Blocks = new Dictionary<short, BlockData>();
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

        public static Item GetItemByName(string name)
        {
            foreach (var item in Items.Values)
            {
                if (item.Name.ToLower() == name.ToLower())
                    return item;
            }
            Debug.Error("GetItemByName error: Wrong name - " + name);
            return null;
        }

        public static bool TryGetItemByName(string name, out Item item)
        {
            item = null;
            foreach (var it in Items.Values)
            {
                if (it.Name.ToLower() == name.ToLower())
                {
                    item = it;
                    return true;
                }
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

        public static Vector2[] GetBlockUVsById(short id)
        {
            if (!Blocks.ContainsKey(id))
                return new Vector2[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            return GetBlockUVsById(id, Face.Left);
        }

        public static Vector2[] GetBlockUVsById(short id, Face face)
        {
            if (!Blocks.ContainsKey(id))
                return new Vector2[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            if (face == Face.Top)
                return Blocks[id].TopUV;
            else if (face == Face.Bottom)
                return Blocks[id].BottomUV;
            return Blocks[id].WallsUV;
        }

        public static Vector2[] GetBlockUVsByIdAndDirection(short id, Face face, Direction direction)
        {
            if (!Blocks.ContainsKey(id))
                return new Vector2[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            return Blocks[id].GetUvsByFaceAndDirection(face, direction);
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
                projectile = null;
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
            projectile = null;
            return false;
        }

        public static void DisposeAll()
        {
            BlocksTexture?.Dispose();
            ItemsTexture?.Dispose();
            LightAtlas?.Dispose();
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
            Projectiles.Clear();
            Blocks.Clear();
            Items.Clear();
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
