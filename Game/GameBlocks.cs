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
            if (!Block.ContainsKey(id)) return new Block(BlockType.Solid, new Vector2(0,0));


            Block block = new Block(Block[id]);

            if (id == 0) block.Type = BlockType.Air;

            return block;
        }

         static GameBlocks()
         {
            AtlasTexture = new Texture2D("Resources/Textures/blocks.png", true);
             
            RegisterBlock(new BlockData("Air", new Vector2(0,0)));

            RegisterBlock(new BlockData("Light Rock", new Vector2(1,3)));
            RegisterBlock(new BlockData("Medium Rock", new Vector2(1, 2)));
            RegisterBlock(new BlockData("Iron Ore", new Vector2(2,3)));
            RegisterBlock(new BlockData("Green Ore", new Vector2(2, 2)));
            RegisterBlock(new BlockData("Alluminium Ore", new Vector2(3,3)));

            RegisterBlock(new BlockData("Alluminium Block", new Vector2(6, 0)));
            RegisterBlock(new BlockData("Window", new Vector2(3,2), true));
            RegisterBlock(new BlockData("Crafting Table", new Vector2(0, 3), false));

            RegisterBlock(new BlockData("Light White", new Vector2(8, 0), false, new Vector3(1,1,1)));
            RegisterBlock(new BlockData("Light Blue", new Vector2(9, 0), false, new Vector3(0,0,1)));
            RegisterBlock(new BlockData("Light Red", new Vector2(10, 0), false, new Vector3(1, 0, 0)));
           

        }
    }
}
