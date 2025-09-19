
using Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;


namespace Spacebox.Game.Resource
{
    internal static class BlockFactory
    {
        private static readonly Dictionary<string, Func<BlockJSON, Block>> BlockCreators =
            new Dictionary<string, Func<BlockJSON, Block>>(StringComparer.OrdinalIgnoreCase)
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
               { "consumer", data => new ConsumerBlock(data) },
                { "storage", data => new StorageBlock(data) }
        };

        public static Block CreateBlockFromId(short id)
        {
            if (!GameAssets.Blocks.ContainsKey(id))
                return new Block();

            BlockJSON data = GameAssets.Blocks[id];
            return CreateBlock(data);
        }

        public static Block CreateBlock(BlockJSON data)
        {
            data.Type = data.Type.ToLower();
            if (BlockCreators.TryGetValue(data.Type, out Func<BlockJSON, Block> creator))
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

        public static void RegisterBlockType(string type, Func<BlockJSON, Block> creator)
        {
            //if (string.IsNullOrWhiteSpace(type))


            //  if (creator == null)
            //      throw new ArgumentNullException(nameof(creator));

            //   BlockCreators[type] = creator;
        }
    }
}
