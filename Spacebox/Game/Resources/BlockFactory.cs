
using Engine;
using Spacebox.Game.Generation;


namespace Spacebox.Game.Resources
{
    internal static class BlockFactory
    {
        private static readonly Dictionary<string, Func<BlockData, Block>> BlockCreators =
            new Dictionary<string, Func<BlockData, Block>>(StringComparer.OrdinalIgnoreCase)
        {
            { "interactive", data => new InteractiveBlock(data) },
            { "crusher", data => new CrusherBlock(data) },
            { "furnace", data => new FurnaceBlock(data) },
            { "disassembler", data => new DisassemblerBlock(data) },
            { "craftingtable", data => new CraftingTableBlock(data) },
            { "radar", data => new RadarBlock(data) },
             { "cable", data => new CableBlock(data) },
             { "block", data => new Block(data) },
              { "generator", data => new GeneratorBlock(data) },
               { "consumer", data => new ConsumerBlock(data) }
        };

        public static Block CreateBlockFromId(short id)
        {
            if (!GameAssets.Blocks.ContainsKey(id))
                return new Block();

            BlockData data = GameAssets.Blocks[id];
            return CreateBlock(data);
        }

        public static Block CreateBlock(BlockData data)
        {
            data.Type = data.Type.ToLower();
            if (BlockCreators.TryGetValue(data.Type, out Func<BlockData, Block> creator))
                return creator(data);
          
            return new Block(data);
        }

        public static bool ValidateBlockType(string type)
        {
            type = type.ToLower();
            return BlockCreators.ContainsKey(type);
        }

        public static string[] GetBlockTypes()
        {
            return BlockCreators.Keys.ToArray();
        }

        public static void RegisterBlockType(string type, Func<BlockData, Block> creator)
        {
            //if (string.IsNullOrWhiteSpace(type))
              

          //  if (creator == null)
          //      throw new ArgumentNullException(nameof(creator));

         //   BlockCreators[type] = creator;
        }
    }
}
