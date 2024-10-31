using OpenTK.Mathematics;


namespace Spacebox.Game
{
    public class BlockData
    {
        public short Id;
        public string Name;
        public Vector2 TextureCoords;
        public bool IsTransparent { get; set; } = false;


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
    }

    public static class GameBlocks
    {
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
            RegisterBlock(new BlockData("Air", new Vector2(0,0)));

            RegisterBlock(new BlockData("Light Rock", new Vector2(1,2)));
            RegisterBlock(new BlockData("Ore Rock", new Vector2(2, 2)));

            RegisterBlock(new BlockData("Alluminium Block", new Vector2(6, 0)));
            RegisterBlock(new BlockData("Window", new Vector2(3,2), true));

            RegisterBlock(new BlockData("Light Source", new Vector2(9, 0)));
            RegisterBlock(new BlockData("Light Source2", new Vector2(10, 0)));
        }
    }
}
