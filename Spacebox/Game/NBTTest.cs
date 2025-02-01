using SharpNBT;
using Engine;
namespace Spacebox.Game
{
    public class NBTTest
    {
        private const string FilePath = "test_world.nbt";

        public NBTTest()
        {
            CreateAndSaveNBT();
            LoadAndReadNBT();
        }

        private void CreateAndSaveNBT()
        {
            var root = new CompoundTag("Root");
            root.Add( new IntTag("Version", 1));
            root.Add( new StringTag("WorldName", "SpaceboxWorld"));
            root.Add(new LongTag("Seed", 1234567890L));

            var chunksList = new ListTag("Chunks", TagType.Compound);

            var chunk1 = new CompoundTag("Chunk000");
            chunk1.Add( new IntTag("ChunkX", 0));
            chunk1.Add( new IntTag("ChunkY", 0));
            chunk1.Add( new IntTag("ChunkZ", 0));
            chunk1.Add( new StringTag("Biome", "Plains"));

            var chunk2 = new CompoundTag("Chunk000");
            chunk2.Add("ChunkX", new IntTag("ChunkX", 1));
            chunk2.Add("ChunkY", new IntTag("ChunkY", 0));
            chunk2.Add("ChunkZ", new IntTag("ChunkZ", 0));
            chunk2.Add("Biome", new StringTag("Biome", "Plains"));

            chunksList.Add(chunk1);
            chunksList.Add(chunk2);
            root.Add(chunksList);

            var lis = new ListTag("Data", TagType.Int);
            lis.Add(new IntTag("0", 0));
            lis.Add(new IntTag("0", 1));
            lis.Add(new IntTag("0", 2));

            root.Add(lis);

            var x = NbtFile.OpenWrite(FilePath, FormatOptions.Java, CompressionType.None);
            x.WriteTag(root);
            x.Dispose();



            Console.WriteLine($"NBT saved: {FilePath}");
        }

        private void LoadAndReadNBT()
        {
            if (!File.Exists(FilePath))
            {
                Console.WriteLine($"File {FilePath} was not found.");
                return;
            }

            TagReader x = NbtFile.OpenRead(FilePath, FormatOptions.Java, CompressionType.None);

            CompoundTag root = (CompoundTag) x.ReadTag();


            Console.WriteLine($"Root: {root.Name}");

            

            if(root.TryGetValue("Version", out IntTag val))
            {
                Console.WriteLine($"Version: {val.Name} {val.Value}");
            }
            else
            {

            }
            Console.WriteLine(root.PrettyPrinted());



            /*
            if (root.TryGet("WorldName", out StringTag worldName))
            {
                Console.WriteLine($"WorldName: {worldName.Value}");
            }

            if (root.TryGet("Seed", out LongTag seed))
            {
                Console.WriteLine($"Seed: {seed.Value}");
            }

            if (root.TryGet("Chunks", out ListTag chunksList))
            {
                Console.WriteLine("Chunks:");
                foreach (var chunkTag in chunksList)
                {
                    if (chunkTag is CompoundTag chunk)
                    {
                        chunk.TryGet("ChunkX", out IntTag chunkX);
                        chunk.TryGet("ChunkZ", out IntTag chunkZ);
                        chunk.TryGet("Biome", out StringTag biome);

                        Console.WriteLine($"\tChunk Coordinates: ({chunkX?.Value}, {chunkZ?.Value})");
                        Console.WriteLine($"\tBiome: {biome?.Value}");
                    }
                }
            }*/
        }
    }
}
