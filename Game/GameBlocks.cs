using OpenTK.Mathematics;
using Spacebox.Common;


namespace Spacebox.Game
{
    public class BlockData
    {
        public short Id;
        public string Name;
        public Vector2 TextureCoords;
        public bool IsTransparent { get; set; } = false;
        public Vector3 LightColor { get; set; } = new Vector3(0,0,0);

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

    public static class GameBlocks
    {

        public static Texture2D AtlasTexture { get; set; }


        public static short MaxBlockId = -1;
        public static Dictionary<short, BlockData> Block = new Dictionary<short, BlockData>();


        private static void RegisterBlock(BlockData blockData)
        {
            MaxBlockId++;

            blockData.Id = MaxBlockId;
            Block.Add(blockData.Id, blockData);
                        
        }

        public static Block CreateFromId(short id)
        {
            if (!Block.ContainsKey(id)) return new Block(new Vector2(0,0));


            Block block = new Block(Block[id]);

            //if (id == 0) block.Type = BlockType.Air;

            return block;
        }

         static GameBlocks()
         {
            AtlasTexture = new Texture2D("Resources/Textures/myBlocks.png", true);
             
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

            RegisterBlock(new BlockData("Wires Pro", new Vector2(4, 2), true));


        }
    }
}
