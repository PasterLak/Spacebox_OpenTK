using SharpNBT;

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
            root.Add("Version", new IntTag("Version", 1));
            root.Add("WorldName", new StringTag("WorldName", "SpaceboxWorld"));
            root.Add("Seed", new LongTag("Seed", 1234567890L));

            var chunksList = new ListTag("Chunks", TagType.Compound);

            var chunk1 = new CompoundTag("Chunk000");
            chunk1.Add("ChunkX", new IntTag("ChunkX", 0));
            chunk1.Add("ChunkY", new IntTag("ChunkY", 0));
            chunk1.Add("ChunkZ", new IntTag("ChunkZ", 0));
            chunk1.Add("Biome", new StringTag("Biome", "Plains"));

            chunksList.Add(chunk1);
            root.Add(chunksList);

          
            var x = NbtFile.OpenWrite(FilePath, FormatOptions.Java, CompressionType.None);
            x.WriteTag(root);
            x.Dispose();



            Console.WriteLine($"NBT saved: {FilePath}");
        }

        private void LoadAndReadNBT()
        {
            if (!File.Exists(FilePath))
            {
                Console.WriteLine($"Файл {FilePath} не найден.");
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
