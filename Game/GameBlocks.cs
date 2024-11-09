using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.GUI;


namespace Spacebox.Game
{
    public class BlockData
    {
        public short Id;
        public string Name;
        //public Vector2 TextureCoords; // replace with byte

        public Vector2Byte WallsUVIndex = new Vector2Byte(0,1);
        public Vector2Byte TopUVIndex = new Vector2Byte(0, 0);
        public Vector2Byte BottomUVIndex = new Vector2Byte(0, 0);

        public Vector2[] WallsUV;
        public Vector2[] TopUV;
        public Vector2[] BottomUV;
        public bool IsTransparent { get; set; } = false;
        public Vector3 LightColor { get; set; } = new Vector3(0,0,0);

        public BlockItem Item;

        public readonly bool AllSidesAreSame = false;

        private Dictionary<Direction, Vector2[]> TopUvsByDirection;
        private Dictionary<Direction, Vector2[]> BottomUvsByDirection;
        private Dictionary<Direction, Vector2[]> LeftUvsByDirection;
        private Dictionary<Direction, Vector2[]> RightUvsByDirection;
        private Dictionary<Direction, Vector2[]> FrontUvsByDirection;
        private Dictionary<Direction, Vector2[]> BackUvsByDirection;

        public BlockData(string name, Vector2Byte textureCoords)
        {
            Name = name;

            WallsUVIndex = textureCoords;
            TopUVIndex = textureCoords;
            BottomUVIndex = textureCoords;

            AllSidesAreSame = true;
        }

        public BlockData(string name, Vector2Byte textureCoords, bool isTransparent)
        {
            Name = name;

            WallsUVIndex = textureCoords;
            TopUVIndex = textureCoords;
            BottomUVIndex = textureCoords;

            IsTransparent = isTransparent;
            AllSidesAreSame = true;
        }

        public BlockData(string name, Vector2Byte textureCoords, bool isTransparent, Vector3 color)
        {
            Name = name;

            WallsUVIndex = textureCoords;
            TopUVIndex = textureCoords;
            BottomUVIndex = textureCoords;

            IsTransparent = isTransparent;
            LightColor = color;
            AllSidesAreSame = true;
        }

        public BlockData(string name, Vector2Byte textureCoords, Vector2Byte topCoords)
        {
            Name = name;

            WallsUVIndex = textureCoords;
            TopUVIndex = topCoords;
            BottomUVIndex = textureCoords;
        }

        public BlockData(string name, Vector2Byte textureCoords,  Vector2Byte topCoords,bool isTransparent)
        {
            Name = name;

            WallsUVIndex = textureCoords;
            TopUVIndex = topCoords;
            BottomUVIndex = textureCoords;

            IsTransparent = isTransparent;
        }

        public BlockData(string name, Vector2Byte textureCoords, Vector2Byte topCoords, bool isTransparent, Vector3 color)
        {
            Name = name;

            WallsUVIndex = textureCoords;
            TopUVIndex = topCoords;
            BottomUVIndex = textureCoords;

            IsTransparent = isTransparent;
            LightColor = color;
        }

        public BlockData(string name, Vector2Byte textureCoords, Vector2Byte topCoords, Vector2Byte bottomCoords)
        {
            Name = name;

            WallsUVIndex = textureCoords;
            TopUVIndex = topCoords;
            BottomUVIndex = bottomCoords;
        }

        public BlockData(string name, Vector2Byte textureCoords, Vector2Byte topCoords, Vector2Byte bottomCoords, bool isTransparent)
        {
            Name = name;

            WallsUVIndex = textureCoords;
            TopUVIndex = topCoords;
            BottomUVIndex = bottomCoords;

            IsTransparent = isTransparent;
        }

        public BlockData(string name, Vector2Byte textureCoords, Vector2Byte topCoords, Vector2Byte bottomCoords, bool isTransparent, Vector3 color)
        {
            Name = name;

            WallsUVIndex = textureCoords;
            TopUVIndex = topCoords;
            BottomUVIndex = bottomCoords;

            IsTransparent = isTransparent;
            LightColor = color;
        }


        public Vector2[] GetUvsByFaceAndDirection(Face face, Direction direction)
        {
            if(AllSidesAreSame)
            {
                return WallsUV;
            }
            
             if (face == Face.Bottom)
            {
                return BottomUvsByDirection[direction];
            }
            else if (face == Face.Left)
            {
                return LeftUvsByDirection[direction];
            }
            else if (face == Face.Right)
            {
                return RightUvsByDirection[direction];
            }
            else if (face == Face.Front)
            {
                return FrontUvsByDirection[direction];
            }
            else if (face == Face.Back)
            {
                return BackUvsByDirection[direction];
            }
            else
            {
                return TopUvsByDirection[direction];
            }
        }

        public void CacheUVsByDirection()
        {
            if (AllSidesAreSame) return;

            TopUvsByDirection = new Dictionary<Direction, Vector2[]>();
            BottomUvsByDirection = new Dictionary<Direction, Vector2[]>();
          
            LeftUvsByDirection = new Dictionary<Direction, Vector2[]>();
            RightUvsByDirection = new Dictionary<Direction, Vector2[]>();
            FrontUvsByDirection = new Dictionary<Direction, Vector2[]>();
            BackUvsByDirection = new Dictionary<Direction, Vector2[]>();

            foreach (Direction dir in Enum.GetValues(typeof(Direction)))
            {
                if (dir == Direction.Up)
                {
                    TopUvsByDirection.Add(dir, TopUV);
                    BottomUvsByDirection.Add(dir, BottomUV);

                    LeftUvsByDirection.Add(dir, WallsUV);
                    RightUvsByDirection.Add(dir, WallsUV);
                    FrontUvsByDirection.Add(dir, WallsUV);
                    BackUvsByDirection.Add(dir, WallsUV);
                }
                else if (dir == Direction.Down)
                {
                    TopUvsByDirection.Add(dir, Rotate180( BottomUV));
                    BottomUvsByDirection.Add(dir, TopUV);

                    LeftUvsByDirection.Add(dir, Rotate180(WallsUV));
                    RightUvsByDirection.Add(dir, Rotate180(WallsUV));
                    FrontUvsByDirection.Add(dir, Rotate180(WallsUV));
                    BackUvsByDirection.Add(dir, Rotate180(WallsUV));
                }
                else if (dir == Direction.Left)
                {
                    TopUvsByDirection.Add(dir, Rotate90Right( WallsUV));
                    BottomUvsByDirection.Add(dir, Rotate90Right(WallsUV));

                    LeftUvsByDirection.Add(dir, TopUV);
                    RightUvsByDirection.Add(dir, BottomUV);
                    FrontUvsByDirection.Add(dir, Rotate90Right(WallsUV));
                    BackUvsByDirection.Add(dir, Rotate90Left(WallsUV));
                }
                else if (dir == Direction.Right)
                {
                    TopUvsByDirection.Add(dir, Rotate90Left(WallsUV));
                    BottomUvsByDirection.Add(dir, Rotate90Left(WallsUV));

                    LeftUvsByDirection.Add(dir, BottomUV);
                    RightUvsByDirection.Add(dir, TopUV);
                    FrontUvsByDirection.Add(dir, Rotate90Left(WallsUV));
                    BackUvsByDirection.Add(dir, Rotate90Right(WallsUV));
                }
                else if (dir == Direction.Forward)
                {
                    TopUvsByDirection.Add(dir, Rotate180(WallsUV));
                    BottomUvsByDirection.Add(dir, WallsUV);

                    LeftUvsByDirection.Add(dir, Rotate90Left(WallsUV));
                    RightUvsByDirection.Add(dir, Rotate90Right(WallsUV));
                    FrontUvsByDirection.Add(dir, TopUV);
                    BackUvsByDirection.Add(dir, BottomUV);
                }
                else 
                {
                    TopUvsByDirection.Add(dir, WallsUV);
                    BottomUvsByDirection.Add(dir, Rotate180(WallsUV));

                    LeftUvsByDirection.Add(dir, Rotate90Right(WallsUV));
                    RightUvsByDirection.Add(dir, Rotate90Left( WallsUV));
                    FrontUvsByDirection.Add(dir, BottomUV);
                    BackUvsByDirection.Add(dir, TopUV);
                }

              

            }
        }

        public  Vector2[] Rotate90Right(Vector2[] uvs)
        {
            if (uvs.Length != 4)
                return WallsUV;

            return new Vector2[]
            {
                uvs[3],
                uvs[0],
                uvs[1],
                uvs[2]
            };
        }

        public  Vector2[] Rotate90Left(Vector2[] uvs)
        {
            if (uvs.Length != 4)
                return WallsUV;

            return new Vector2[]
            {
                uvs[1],
                uvs[2],
                uvs[3],
                uvs[0]
            };
        }

        public  Vector2[] Rotate180(Vector2[] uvs)
        {
            if (uvs.Length != 4)
                return WallsUV;

            return new Vector2[]
            {
                uvs[2],
                uvs[3],
                uvs[0],
                uvs[1]
            };
        }



        //---
    }

    

    // --------------------------------------------------
    public static class GameBlocks
    {

        public static Texture2D BlocksTexture { get; set; }
        public static Texture2D ItemsTexture { get; set; }


        public static short MaxBlockId = -1;
        public static short MaxItemId = -1;

        public static Dictionary<short, BlockData> Block = new Dictionary<short, BlockData>();
        public static Dictionary<short, Item> Item = new Dictionary<short, Item>();
        public static Dictionary<short, ItemModel> ItemModels = new Dictionary<short, ItemModel>();

        public static Dictionary<short, Texture2D> ItemIcon = new Dictionary<short, Texture2D>();
        public static Dictionary<short, Texture2D> BlockDust = new Dictionary<short, Texture2D>();
      
        private static void RegisterBlock(BlockData blockData)
        {
            MaxBlockId++;

            blockData.Id = MaxBlockId;
            Block.Add(blockData.Id, blockData);

            CacheBlockUVs(blockData);

            blockData.CacheUVsByDirection();

            RegisterItem(blockData);
            CreateDust(blockData);
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

        private static void RegisterItem(Item item)
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
            BlocksTexture = new Texture2D("Resources/Textures/myBlocks.png", true);
            ItemsTexture = new Texture2D("Resources/Textures/items.png", true);

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
