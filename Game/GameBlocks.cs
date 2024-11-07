﻿using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.GUI;


namespace Spacebox.Game
{
    public class BlockData
    {
        public short Id;
        public string Name;
        public Vector2 TextureCoords; // replace with byte
        public bool IsTransparent { get; set; } = false;
        public Vector3 LightColor { get; set; } = new Vector3(0,0,0);

        public ItemBlock Item;
        public BlockData(string name, Vector2 textureCoords)
        {
            Name = name;
            TextureCoords = textureCoords;
        }

        public BlockData(string name, Vector2 textureCoords, bool isTransparent)
        {
            Name = name;
            TextureCoords = textureCoords;
            IsTransparent = isTransparent;
        }

        public BlockData(string name, Vector2 textureCoords, bool isTransparent, Vector3 color)
        {
            Name = name;
            TextureCoords = textureCoords;
            IsTransparent = isTransparent;
            LightColor = color;
        }
    }

    public class ItemBlock : Item
    {

        public short BlockId;

        public ItemBlock(short blockId, short id, byte stackSize, string name) : base(id, stackSize, name)
        {
            BlockId = blockId;
        }
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
       

        public static Dictionary<short, Texture2D> ItemIcon = new Dictionary<short, Texture2D>();

        private static void RegisterBlock(BlockData blockData)
        {
            MaxBlockId++;

            blockData.Id = MaxBlockId;
            Block.Add(blockData.Id, blockData);

            RegisterItem(blockData);

        }

        private static void RegisterItem(BlockData blockData)
        {
            MaxItemId++;

            ItemBlock item = new ItemBlock(blockData.Id, MaxItemId, 64, blockData.Name);

            blockData.Item = item;

            Item.Add(item.Id, item);

            CacheIcon(blockData);

        }

        private static void RegisterItem(Item item)
        {
            MaxItemId++;

            item.Id = MaxItemId;

            Item.Add(item.Id, item);

            CacheIcon(item, (byte)item.TextureCoord.X, (byte)item.TextureCoord.Y);

        }

        public static Item GetItemByName(string name)
        {
            foreach(var item in Item.Values)
            {
                if(item.Name == name) return item;
            }

            Debug.Error("GetItemByName error: Wrong name - " + name);
            return null;
        }

        public static BlockData GetBlockDataById(short id)
        {
            if (!Block.ContainsKey(id)) return Block[0];


            return Block[id];
        }

        public static Block CreateFromId(short id)
        {
            if (!Block.ContainsKey(id)) return new Block(new Vector2(0,0));


            Block block = new Block(Block[id]);

            //if (id == 0) block.Type = BlockType.Air;

            return block;
        }

        private static void CacheIcon(BlockData blockData)
        {
            Texture2D texture = IsometricIcon.CreateIsometricIcon(
                UVAtlas.GetBlockTexture(BlocksTexture, 
                (int)blockData.TextureCoords.X, (int)blockData.TextureCoords.Y)
                );
            

            ItemIcon.Add(blockData.Item.Id, texture);
        }

        private static void CacheIcon(Item item, byte x, byte y)
        {
            Texture2D texture =
                UVAtlas.GetBlockTexture(ItemsTexture, x, y);

            texture.FlipY();

            item.IconTextureId = texture.Handle;


            ItemIcon.Add(item.Id, texture);
        }

        static GameBlocks()
         {
            BlocksTexture = new Texture2D("Resources/Textures/myBlocks.png", true);
            ItemsTexture = new Texture2D("Resources/Textures/items.png", true);

            RegisterBlock(new BlockData("Air", new Vector2(0,0)));

            RegisterBlock(new BlockData("Light Rock", new Vector2(1,3)));
            RegisterBlock(new BlockData("Medium Rock", new Vector2(1, 2)));
            RegisterBlock(new BlockData("Heavy Rock", new Vector2(5,3)));
            RegisterBlock(new BlockData("Ice", new Vector2(4, 3))); // 4

            RegisterBlock(new BlockData("Iron Ore", new Vector2(2,3))); // 5
            RegisterBlock(new BlockData("Alluminium Ore", new Vector2(3, 3)));
            RegisterBlock(new BlockData("Green Ore", new Vector2(2, 2)));
            RegisterBlock(new BlockData("Pink Ore", new Vector2(6,3)));


            RegisterBlock(new BlockData("Analyzer", new Vector2(4, 0))); //9
          
            RegisterBlock(new BlockData("Alluminium Light Block", new Vector2(6, 1)));
            RegisterBlock(new BlockData("Alluminium Medium Block", new Vector2(7, 1)));
            RegisterBlock(new BlockData("Alluminium Heavy Block", new Vector2(8, 1)));

            RegisterBlock(new BlockData("Iron Light Block", new Vector2(9, 1)));
            RegisterBlock(new BlockData("Iron Medium Block", new Vector2(10, 1)));
            RegisterBlock(new BlockData("Iron Heavy Block", new Vector2(11, 1)));

            RegisterBlock(new BlockData("Titanium Light Block", new Vector2(12, 1)));
            RegisterBlock(new BlockData("Titanium Medium Block", new Vector2(13, 1)));
            RegisterBlock(new BlockData("Titanium Heavy Block", new Vector2(14, 1)));

            RegisterBlock(new BlockData("Window", new Vector2(3,2), true)); // 19
            RegisterBlock(new BlockData("Crafting Table", new Vector2(0, 3), false));
            RegisterBlock(new BlockData("Wire", new Vector2(0, 2)));
            RegisterBlock(new BlockData("Radar", new Vector2(0, 4)));


            RegisterBlock(new BlockData("Light White", new Vector2(8, 0), false, new Vector3(1,1,1)));
            RegisterBlock(new BlockData("Light Blue", new Vector2(9, 0), false, new Vector3(0,0,1)));
            RegisterBlock(new BlockData("Light Red", new Vector2(10, 0), false, new Vector3(1, 0, 0)));

            RegisterBlock(new BlockData("Light Yellow", new Vector2(11, 0), false, new Vector3(1, 216/255f, 0)));
            RegisterBlock(new BlockData("Light Cyan", new Vector2(12, 0), false, new Vector3(0,1,1)));

            RegisterBlock(new BlockData("Light Orange", new Vector2(13, 0), false, new Vector3(1, 106 / 255f, 0)));
            RegisterBlock(new BlockData("Light Green", new Vector2(14, 0), false, new Vector3(0, 1, 0)));

            RegisterBlock(new BlockData("Wires Pro", new Vector2(4, 2), true));

            RegisterBlock(new BlockData("AI Core", new Vector2(6, 2), true));
            RegisterBlock(new BlockData("Block", new Vector2(7,2)));



            RegisterItem(new Item(1, "Drill", 0,0));
            RegisterItem(new Item(1, "Weapone", 1,0));
            RegisterItem(new Item(64, "Powder", 2,0));
            RegisterItem(new Item(64, "Iron Lens", 1,1));

        }
    }
}
