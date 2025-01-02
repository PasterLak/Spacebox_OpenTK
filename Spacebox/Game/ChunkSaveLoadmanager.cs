using System.Text.Json;
using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Resources;

namespace Spacebox.Game
{
    public static class ChunkSaveLoadManager
    {
        private static readonly string SaveDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Worlds");

        static ChunkSaveLoadManager()
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }
        }

        public static void SaveChunk(Chunk chunk, string worldFolder)
        {
            try
            {
                short[] blockIds = GetBlockIdsAs1D(chunk.Blocks);
                List<DirectionData> directions = new List<DirectionData>();
                for (int i = 0; i < blockIds.Length; i++)
                {
                    Block block = chunk.Blocks[i / (Chunk.Size * Chunk.Size), (i / Chunk.Size) % Chunk.Size, i % Chunk.Size];
                    if (block.Direction != Direction.Up)
                    {
                        directions.Add(new DirectionData { Index = i, Direction = block.Direction });
                    }
                }
                ChunkData data = new ChunkData
                {
                    ModId = GameSetLoader.ModInfo.ModId,
                    PositionX = chunk.PositionWorld.X,
                    PositionY = chunk.PositionWorld.Y,
                    PositionZ = chunk.PositionWorld.Z,
                    BlockIds = blockIds,
                    Directions = directions
                };
                string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                string filePath = GetChunkFilePath(chunk.PositionWorld, Path.Combine(SaveDirectory, worldFolder));
                File.WriteAllText(filePath, jsonString);
                Debug.Log($"Chunk saved at {filePath}");
            }
            catch (Exception ex)
            {
                Debug.Error($"Error saving chunk: {ex.Message}");
            }
        }

        public static Chunk LoadChunkFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.Error($"Chunk file not found: {filePath}");
                    return null;
                }

                string jsonString = File.ReadAllText(filePath);
                ChunkData data = JsonSerializer.Deserialize<ChunkData>(jsonString);
                if (data == null)
                {
                    Debug.Error($"Failed to deserialize chunk data from {filePath}");
                    return null;
                }

                Block[,,] loadedBlocks = ReconstructBlocksFrom1D(data.BlockIds);
                foreach (var directionData in data.Directions)
                {
                    int index = directionData.Index;
                    int x = index / (Chunk.Size * Chunk.Size);
                    int y = (index / Chunk.Size) % Chunk.Size;
                    int z = index % Chunk.Size;
                    loadedBlocks[x, y, z].Direction = directionData.Direction;
                }

                Vector3 loadedPositionWorld = new Vector3(data.PositionX, data.PositionY, data.PositionZ);
                Chunk chunk = new Chunk(loadedPositionWorld,null, loadedBlocks, isLoaded: true);
                Debug.Success($"Chunk loaded from {filePath}");
                return chunk;
            }
            catch (Exception ex)
            {
                Debug.Error($"Error loading chunk from {filePath}: {ex.Message}");
                return null;
            }
        }

        public static WorldChunks LoadWorld(string worldName)
        {
            try
            {
              
                string[] worldFolders = Directory.GetDirectories(SaveDirectory);

                foreach (string worldFolder in worldFolders)
                {
                    string worldJsonPath = Path.Combine(worldFolder, "world.json");
                    if (File.Exists(worldJsonPath))
                    {
                        string jsonContent = File.ReadAllText(worldJsonPath);
                        WorldInfo worldInfo = JsonSerializer.Deserialize<WorldInfo>(jsonContent);
                        if (worldInfo != null && string.Equals(worldInfo.Name, worldName, StringComparison.OrdinalIgnoreCase))
                        {
                          
                            string chunksDirectory = Path.Combine(worldFolder, "Chunks");
                            if (!Directory.Exists(chunksDirectory))
                            {
                                Debug.Error($"Chunks directory not found for world '{worldName}' at {chunksDirectory}");
                                return null;
                            }

                            string[] chunkFiles = Directory.GetFiles(chunksDirectory, "chunk_*.json");
                            Dictionary<Vector3, Chunk> loadedChunks = new Dictionary<Vector3, Chunk>();

                            foreach (string chunkFile in chunkFiles)
                            {
                                string fileName = Path.GetFileNameWithoutExtension(chunkFile); //"chunk_X_Y_Z"
                                Vector3 chunkPosition = ParseChunkPosition(fileName);
                                if (chunkPosition == Vector3.Zero)
                                {
                                   // Debug.Error($"Invalid chunk file name format: {fileName}");
                                   // continue;
                                }

                                Chunk loadedChunk = LoadChunkFromFile(chunkFile);
                                if (loadedChunk != null)
                                {
                                    loadedChunks.Add(chunkPosition, loadedChunk);
                                }
                            }

                         
                            WorldChunks loadedWorld = new WorldChunks
                            {
                                Info = worldInfo,
                                Chunks = loadedChunks
                            };

                            Debug.Success($"World '{worldName}' loaded successfully with {loadedChunks.Count} chunks.");
                            return loadedWorld;
                        }
                    }
                }

                Debug.Error($"World '{worldName}' not found in '{SaveDirectory}'.");
                return null;
            }
            catch (Exception ex)
            {
                Debug.Error($"Error loading world '{worldName}': {ex.Message}");
                return null;
            }
        }


        private static Vector3 ParseChunkPosition(string fileName)
        {
            string[] parts = fileName.Split('_');
            if (parts.Length != 4)
            {
                return Vector3.Zero;
            }

            if (int.TryParse(parts[1], out int x) &&
                int.TryParse(parts[2], out int y) &&
                int.TryParse(parts[3], out int z))
            {
                return new Vector3(x, y, z);
            }

            return Vector3.Zero;
        }


        private static string GetChunkFilePath(Vector3 position, string worldFolder)
        {
            string chunksDirectory = Path.Combine(worldFolder, "Chunks");
            return Path.Combine(chunksDirectory, $"chunk_{position.X}_{position.Y}_{position.Z}.json");
        }

        private static short[] GetBlockIdsAs1D(Block[,,] blocks)
        {
            short[] ids = new short[Chunk.Size * Chunk.Size * Chunk.Size];
            int index = 0;
            for (int x = 0; x < Chunk.Size; x++)
                for (int y = 0; y < Chunk.Size; y++)
                    for (int z = 0; z < Chunk.Size; z++)
                    {
                        ids[index++] = blocks[x, y, z].BlockId;
                    }
            return ids;
        }


        private static Block[,,] ReconstructBlocksFrom1D(short[] ids)
        {
            if (ids.Length != Chunk.Size * Chunk.Size * Chunk.Size)
            {
                throw new ArgumentException("Invalid number of Block IDs for chunk reconstruction.");
            }
            Block[,,] blocks = new Block[Chunk.Size, Chunk.Size, Chunk.Size];
            int index = 0;
            for (int x = 0; x < Chunk.Size; x++)
                for (int y = 0; y < Chunk.Size; y++)
                    for (int z = 0; z < Chunk.Size; z++)
                    {
                        blocks[x, y, z] = GameBlocks.CreateBlockFromId(ids[index++]);
                    }
            return blocks;
        }

        private class ChunkData
        {
            public string ModId { get; set; } = "default";
            public float PositionX { get; set; }
            public float PositionY { get; set; }
            public float PositionZ { get; set; }
            public short[] BlockIds { get; set; }
            public List<DirectionData> Directions { get; set; }
        }


        private class DirectionData
        {
            public int Index { get; set; }
            public Direction Direction { get; set; }
        }

        public class WorldChunks
        {
            public WorldInfo Info { get; set; }
            public Dictionary<Vector3, Chunk> Chunks { get; set; } = new Dictionary<Vector3, Chunk>();
        }
    }
}
