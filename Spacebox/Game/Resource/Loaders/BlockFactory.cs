
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Blocks.Components;
using Spacebox.Game.Generation.Oxygen;
using Spacebox.Game.Generation.Doors;


namespace Spacebox.Game.Resource
{
    internal static class BlockFactory
    {
        private static readonly Dictionary<string, Func<BlockData, Block>> BlockCreators =
            new Dictionary<string, Func<BlockData, Block>>(StringComparer.OrdinalIgnoreCase)
        {
            { "interactive", data => new InteractiveBlock(data) },
            { "crusher", ComponentBlocks.Create },
            { "analyzer", data => new AnalyzerBlock(data) },
            { "mineral", data => new MineralBlock(data) },
            { "furnace", ComponentBlocks.Create },
            { "disassembler", data => new DisassemblerBlock(data) },
            { "craftingtable", data => new CraftingTableBlock(data) },
            { "radar", data => new RadarBlock(data) },
             { "cable", data => new CableBlock(data) },
             { "block", data => new Block(data) },
              { "generator", ComponentBlocks.Create },
               { "consumer", data => new ConsumerBlock(data) },
                { "storage", data => new StorageBlock(data as StorageBlockData) },
                { "oxygengenerator", data => new OxygenGeneratorBlock(data) },
                { "door", data => new DoorBlock(data) },
                { "sign", data => new SignBlock(data) },
                { "frame", data => new PictureFrameBlock(data) },
                { "gravity", data => new GravityGeneratorBlock(data) },
                { "claim", data => new ClaimBeaconBlock(data) },
                { "dnareconstructor", data => new DnaReconstructorBlock(data) },
                { "explosive", data => new ExplosiveBlock(data) },
                { "pipe", data => new PipeBlock(data) },
                { "thruster", data => new ThrusterBlock(data) },
                { "shipconsole", data => new ShipConsoleBlock(data) },
                { "cannon", data => new CannonBlock(data) },
                { "shipcamera", data => new ShipCameraBlock(data) },
                { InvisibleBlock.TypeName, data => new InvisibleBlock(data) },
                { BlockComponentRegistry.ModularType, ComponentBlocks.Create }
        };

        public static Block CreateBlockFromId(short id)
        {
            if (!GameAssets.Blocks.ContainsKey(id))
                return new Block();

            return CreateBlock(GameAssets.Blocks[id]);
        }

        public static Block CreateBlock(BlockData data)
        {
            data.Type = data.Type.ToLower();
            if (BlockCreators.TryGetValue(data.Type, out Func<BlockData, Block> creator))
                return creator(data);

            return new Block(data);
        }

        public static IEnumerable<string> Types => BlockCreators.Keys;

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
