using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.GUI;


namespace Spacebox.Game
{
    

    public static class GameBlocks
    {

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

        public static void RegisterItem(Item item)
        {
            MaxItemId++;

            item.Id = MaxItemId;

            Item.Add(item.Id, item);

            byte coordX = (byte)item.TextureCoord.X;
            byte coordY = (byte)item.TextureCoord.Y;

            CacheIcon(item, coordX, coordY);

            GenerateItemModel(coordX, coordY, item.ModelDepth);
        }

        private static void GenerateItemModel(byte coordX, byte coordY, float depth)
        {
            ItemModel model = ItemModelGenerator.GenerateModel(
                ItemsTexture, coordX, coordY, 0.02f, depth / 100f * 2f, true);

            ItemModels.Add(MaxItemId, model);
        }

        public static Item GetItemByName(string name)
        {
            foreach(var item in Item.Values)
            {
                if(item.Name.ToLower() == name.ToLower()) return item;
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
            if (!Block.ContainsKey(id)) return new Block(new Vector2(0,0));


            Block block = new Block(Block[id]);

            //if (id == 0) block.Type = BlockType.Air;

            return block;
        }

        public static Vector2[] GetBlockUVsById(short id)
        {
            if (!Block.ContainsKey(id)) 
                return 
                    new  Vector2[] 
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

            if(face == Face.Top)
            {
                return Block[id].TopUV;
            }
            else if(face == Face.Bottom)
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

            return Block[id].GetUvsByFaceAndDirection(face,direction);
        }

        private static void CreateDust(BlockData block)
        {
            Texture2D texture = BlockDestructionTexture.Generate(block.WallsUVIndex);

            BlockDust.Add(block.Id, texture);
        }

        private static void CacheIcon(BlockData blockData)
        {
            Texture2D texture = IsometricIcon.CreateIsometricIcon(
                UVAtlas.GetBlockTexture(BlocksTexture,
                blockData.WallsUVIndex),
                UVAtlas.GetBlockTexture(BlocksTexture,
                blockData.TopUVIndex)
                );
            

            ItemIcon.Add(blockData.Item.Id, texture);
        }

        private static void CacheIcon(Item item, byte x, byte y)
        {
            Texture2D texture =
                UVAtlas.GetBlockTexture(ItemsTexture, x, y);

            texture.FlipY();
            texture.UpdateTexture(true);
            item.IconTextureId = texture.Handle;


            ItemIcon.Add(item.Id, texture);
        }

        private static void CacheBlockUVs(BlockData block)
        {

            block.WallsUV = UVAtlas.GetUVs(block.WallsUVIndex);
            block.TopUV = UVAtlas.GetUVs(block.TopUVIndex);
            block.BottomUV = UVAtlas.GetUVs(block.BottomUVIndex);
        }

        public static Storage CreateCreativeStorage(byte sizeX)   // 32
        {
            byte sizeY = (byte) (Item.Count / sizeX);

            byte rest = (byte)(Item.Count % sizeX);

            if(rest > 0)
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

                    if(!itemsIds.Contains(i))
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

        static GameBlocks()
         {

            ModLoader.Load("Default");

            return;

            RegisterBlock(new BlockData("Air", new Vector2Byte(0,0)));

            RegisterBlock(new BlockData("Light Rock", new Vector2Byte(1,3)));
            RegisterBlock(new BlockData("Medium Rock", new Vector2Byte(1, 2)));
            RegisterBlock(new BlockData("Heavy Rock", new Vector2Byte(5,3)));
            RegisterBlock(new BlockData("Ice", new Vector2Byte(4, 3))); // 4

            RegisterBlock(new BlockData("Iron Ore", new Vector2Byte(2,3))); // 5
            RegisterBlock(new BlockData("Aluminium Ore", new Vector2Byte(3, 3)));
            RegisterBlock(new BlockData("Green Ore", new Vector2Byte(2, 2)));
            RegisterBlock(new BlockData("Pink Ore", new Vector2Byte(6,3)));


            RegisterBlock(new BlockData("Analyzer", new Vector2Byte(1,4), new Vector2Byte(4,0), new Vector2Byte(7,2))); //9
          
            RegisterBlock(new BlockData("Aluminium Light Block", new Vector2Byte(6, 1)));
            RegisterBlock(new BlockData("Aluminium Medium Block", new Vector2Byte(7, 1)));
            RegisterBlock(new BlockData("Aluminium Heavy Block", new Vector2Byte(8, 1)));

            RegisterBlock(new BlockData("Iron Light Block", new Vector2Byte(9, 1)));
            RegisterBlock(new BlockData("Iron Medium Block", new Vector2Byte(10, 1)));
            RegisterBlock(new BlockData("Iron Heavy Block", new Vector2Byte(11, 1)));

            RegisterBlock(new BlockData("Titanium Light Block", new Vector2Byte(12, 1)));
            RegisterBlock(new BlockData("Titanium Medium Block", new Vector2Byte(13, 1)));
            RegisterBlock(new BlockData("Titanium Heavy Block", new Vector2Byte(14, 1)));

            RegisterBlock(new BlockData("Window", new Vector2Byte(3,2), true)); // 19
            RegisterBlock(new BlockData("Crafting Table", new Vector2Byte(2,4), new Vector2Byte(0, 3), new Vector2Byte(7, 2), false));
            RegisterBlock(new BlockData("Wire", new Vector2Byte(0, 2)));
            RegisterBlock(new BlockData("Radar", new Vector2Byte(1,4), new Vector2Byte(0, 4), new Vector2Byte(7, 2)));


            RegisterBlock(new BlockData("Light White", new Vector2Byte(8, 0), false, new Vector3(1,1,1)));
            RegisterBlock(new BlockData("Light Blue", new Vector2Byte(9, 0), false, new Vector3(0,0,1)));
            RegisterBlock(new BlockData("Light Red", new Vector2Byte(10, 0), false, new Vector3(1, 0, 0)));

            RegisterBlock(new BlockData("Light Yellow", new Vector2Byte(11, 0), false, new Vector3(1, 216/255f, 0)));
            RegisterBlock(new BlockData("Light Cyan", new Vector2Byte(12, 0), false, new Vector3(0,1,1)));

            RegisterBlock(new BlockData("Light Orange", new Vector2Byte(13, 0), false, new Vector3(1, 106 / 255f, 0)));
            RegisterBlock(new BlockData("Light Green", new Vector2Byte(14, 0), false, new Vector3(0, 1, 0)));

            RegisterBlock(new BlockData("Wires Pro", new Vector2Byte(4, 2), true));

            RegisterBlock(new BlockData("AI Core", new Vector2Byte(6, 2), true));
            RegisterBlock(new BlockData("Block", new Vector2Byte(7,2)));


            //  ----------  Items  ----------

            RegisterItem(new DrillItem(1, "Drill", 3,0,1.5f));
            RegisterItem(new WeaponeItem(1, "Weapone", 4,0, 1.5f));
            RegisterItem(new Item(64, "Powder", 2,0));
            RegisterItem(new Item(64, "Iron Lens", 2,1));
            RegisterItem(new Item(64, "Aluminium Panels", 1, 1, 1));
            RegisterItem(new Item(64, "Iron Panels", 0, 1, 1));
            RegisterItem(new Item(64, "Titanium Ingot", 0, 2, 2));
            RegisterItem(new Item(64, "Aluminium Ingot", 1, 2, 2));
            RegisterItem(new Item(64, "Health", 2, 2, 2));
            RegisterItem(new Item(64, "Ice Shards", 3, 1, 1.5f));
            RegisterItem(new Item(64, "Power", 3, 2, 2));
            RegisterItem(new Item(64, "Beer", 4, 1, 3));
            RegisterItem(new Item(64, "Cat", 5,0, 2));

        }
    }
}
