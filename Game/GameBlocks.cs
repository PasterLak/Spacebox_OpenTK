using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.GUI;
using static Spacebox.Common.Utils.Testing;

namespace Spacebox.Game
{

    public static class GameBlocks
    {

        public static bool IsInitialized = false;
        public static string modId = "";
        public static Texture2D BlocksTexture { get; set; }
        public static Texture2D ItemsTexture { get; set; }
        public static Texture2D LightAtlas { get; set; }
        public static Texture2D DustTexture { get; set; }


        public static short MaxBlockId = -1;
        public static short MaxItemId = -1;

        public static Dictionary<short, BlockData> Block = new Dictionary<short, BlockData>();
        public static Dictionary<short, Item> Item = new Dictionary<short, Item>();
        public static Dictionary<short, ItemModel> ItemModels = new Dictionary<short, ItemModel>();

        public static Dictionary<short, Texture2D> ItemIcon = new Dictionary<short, Texture2D>();
        public static Dictionary<short, Texture2D> BlockDust = new Dictionary<short, Texture2D>();
        public static Dictionary<short, AudioClip> ItemSound = new Dictionary<short, AudioClip>();
        public static Dictionary<string, AudioClip> Sounds = new Dictionary<string, AudioClip>();

        public static AtlasTexture AtlasBlocks;
        public static AtlasTexture AtlasItems;


        public static void ProcessBlockDataTexture(BlockData blockData)
        {
            if (AtlasBlocks == null)
            {
                Debug.Error("[GameBlocks] AtlasTexture is not created!");
            }

            blockData.WallsUV = AtlasBlocks.GetUVByName(blockData.Sides);
            blockData.TopUV = AtlasBlocks.GetUVByName(blockData.Top);
            blockData.BottomUV = AtlasBlocks.GetUVByName(blockData.Bottom);

            blockData.WallsUVIndex = AtlasBlocks.GetUVIndexByName(blockData.Sides);
            blockData.TopUVIndex = AtlasBlocks.GetUVIndexByName(blockData.Top);
            blockData.BottomUVIndex = AtlasBlocks.GetUVIndexByName(blockData.Bottom);
        }
        public static void RegisterBlock(BlockData blockData)
        {
            MaxBlockId++;

            blockData.Id = MaxBlockId;
            Block.Add(blockData.Id, blockData);


            CacheBlockUVs(blockData);

            blockData.CacheUVsByDirection();

            RegisterItem(blockData);
            CreateDust(blockData);

            //Debug.Log($"Block was registered {blockData.Id}  {blockData.Name}  {blockData.WallsUVIndex}  {blockData.IsTransparent}");
        }

        private static void RegisterItem(BlockData blockData)
        {
            if (blockData.Id == 0) return;

            MaxItemId++;

            BlockItem item = new BlockItem(blockData.Id, MaxItemId, 64, blockData.Name);

            blockData.Item = item;

            Item.Add(item.Id, item);

            CacheIcon(blockData);

        }

        public static bool TryGetItemSound(short id, out AudioClip clip)
        {
            clip = null;
            if (ItemSound.ContainsKey(id))
            {
                clip = ItemSound[id];
                return true;
            }

            return false;
        }

        public static void RegisterItem(Item item, string spriteName)
        {
            MaxItemId++;

            item.Id = MaxItemId;

            Item.Add(item.Id, item);

            var uvIndex = AtlasItems.GetUVIndexByName(spriteName.ToLower());

            byte coordX = uvIndex.X;
            byte coordY = uvIndex.Y;

            item.TextureCoord = new Vector2i(coordX, coordY);

            CacheIcon(item, coordX, coordY);

            GenerateItemModel(coordX, coordY, item.ModelDepth);

            if (item is ConsumableItem)
            {
                var it = item as ConsumableItem;
                if (!ItemSound.ContainsKey(it.Id))
                {
                    if (Sounds.ContainsKey(it.UseSound))
                    {

                    }
                    ItemSound.Add(it.Id, Sounds[it.UseSound]);
                }
            }
        }

        private static void GenerateItemModel(byte coordX, byte coordY, float depth)
        {
            ItemModel model = ItemModelGenerator.GenerateModel(
                ItemsTexture, coordX, coordY, 0.02f, depth / 100f * 2f, true);

            ItemModels.Add(MaxItemId, model);
        }

        public static Item GetItemByName(string name)
        {
            foreach (var item in Item.Values)
            {
                if (item.Name.ToLower() == name.ToLower()) return item;
            }

            Debug.Error("GetItemByName error: Wrong name - " + name);
            return null;
        }

        public static BlockData GetBlockDataById(short id)
        {
            if (!Block.ContainsKey(id)) return Block[0];


            return Block[id];
        }

        public static Block CreateBlockFromId(short id)
        {
            if (!Block.ContainsKey(id)) return new Block(new Vector2(0, 0));

            BlockData data = Block[id];


            if (data.Type.ToLower() == "interactive")
            {
                return new InteractiveBlock(data);
            }

            //if (id == 0) block.Type = BlockType.Air;

            return new Block(data);
        }

        public static bool TryGetItemByBlockID(int blockID, out Item item)
        {
            item = null;
            blockID--;
            if (Item.ContainsKey((short)blockID))
            {
                item = Item[(short)blockID];
                return true;
            }
            return false;
        }

        public static Vector2[] GetBlockUVsById(short id)
        {
            if (!Block.ContainsKey(id))
                return
                    new Vector2[]
                    {new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f) };

            return GetBlockUVsById(id, Face.Left);
        }

        public static Vector2[] GetBlockUVsById(short id, Face face)
        {
            if (!Block.ContainsKey(id))
                return
                    new Vector2[]
                    {new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f) };

            if (face == Face.Top)
            {
                return Block[id].TopUV;
            }
            else if (face == Face.Bottom)
            {
                return Block[id].BottomUV;
            }

            return Block[id].WallsUV;
        }

        public static Vector2[] GetBlockUVsByIdAndDirection(short id, Face face, Direction direction)
        {
            if (!Block.ContainsKey(id))
                return
                    new Vector2[]
                    {new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f) };

            return Block[id].GetUvsByFaceAndDirection(face, direction);
        }

        private static void CreateDust(BlockData block)
        {
            Texture2D texture = BlockDestructionTexture.Generate(block.WallsUVIndex);
            // texture.SaveToPng("af" + DateTime.Now.Millisecond + ".png");
            BlockDust.Add(block.Id, texture);
        }

        private static void CacheIcon(BlockData blockData)
        {
            Texture2D texture = IsometricIcon.CreateIsometricIcon(
                UVAtlas.GetBlockTexture(BlocksTexture,
                blockData.WallsUVIndex, AtlasBlocks.SizeBlocks),
                UVAtlas.GetBlockTexture(BlocksTexture,
                blockData.TopUVIndex, AtlasBlocks.SizeBlocks)
                );
            texture.UpdateTexture(true);

            ItemIcon.Add(blockData.Item.Id, texture);
        }

        private static void CacheIcon(Item item, byte x, byte y)
        {
            Texture2D texture =
                UVAtlas.GetBlockTexture(ItemsTexture, x, y, AtlasItems.SizeBlocks);

            texture.FlipY();
            texture.UpdateTexture(true);
            item.IconTextureId = texture.Handle;


            ItemIcon.Add(item.Id, texture);
        }

        private static void CacheBlockUVs(BlockData block)
        {
            ProcessBlockDataTexture(block);
        }

        public static Storage CreateCreativeStorage(byte sizeX)   // 32
        {
            byte sizeY = (byte)(Item.Count / sizeX);

            byte rest = (byte)(Item.Count % sizeX);

            if (rest > 0)
            {
                sizeY++;
            }

            Storage storage = new Storage(sizeX, sizeY);

            List<short> itemsIds = new List<short>();
            itemsIds.AddRange(Item.Keys.ToArray<short>());

            //itemsIds.RemoveAt(0);

            short i = 0;

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
                        slot.Item = Item[itemsIds[i]];
                        slot.Count = 1;
                        i++;
                    }
                }

                if (i >= itemsIds.Count) break;
            }

            return storage;
        }

        public static void DisposeAll()
        {
            BlocksTexture?.Dispose();
            ItemsTexture?.Dispose();
            LightAtlas?.Dispose();
            DustTexture?.Dispose();
            foreach (var texture in ItemIcon.Values)
            {
                texture.Dispose();
            }
            foreach (var texture in BlockDust.Values)
            {
                texture.Dispose();
            }
            foreach (var c in ItemSound.Values)
            {
                if (c != null)
                {
                    c.Dispose();

                }
            }
            foreach (var c in Sounds.Values)
            {
                if (c != null) c.Dispose();
            }
            Block.Clear();
            Item.Clear();
            ItemModels.Clear();
            ItemIcon.Clear();
            BlockDust.Clear();
            AtlasBlocks.Dispose();
            AtlasBlocks = null;
            AtlasItems.Dispose();
            AtlasItems = null;
            MaxBlockId = -1;
            MaxItemId = -1;
            modId = "";
            IsInitialized = false;

        }

    }
}
