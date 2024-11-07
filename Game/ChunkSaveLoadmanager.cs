using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public static class ChunkSaveLoadManager
    {
        private static readonly string SaveDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Chunks");

        static ChunkSaveLoadManager()
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }
        }

        /// <summary>
        /// Saves the chunk data by serializing its position and block IDs as a one-dimensional array.
        /// </summary>
        /// <param name="chunk">The chunk to save.</param>
        public static void SaveChunk(Chunk chunk)
        {
            try
            {
                // Extract Block IDs from the chunk as a one-dimensional array
                short[] blockIds = GetBlockIdsAs1D(chunk.Blocks);

                // Create a serializable data structure
                ChunkData data = new ChunkData
                {
                    PositionX = chunk.Position.X,
                    PositionY = chunk.Position.Y,
                    PositionZ = chunk.Position.Z,
                    BlockIds = blockIds
                };

                // Serialize to JSON
                string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Determine the file path
                string filePath = GetChunkFilePath(chunk.Position);

                // Write to file
                File.WriteAllText(filePath, jsonString);
                Debug.Log($"Chunk saved at {filePath}");
            }
            catch (Exception ex)
            {
                Debug.Error($"Error saving chunk: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the chunk data by deserializing its position and block IDs from a one-dimensional array.
        /// </summary>
        /// <param name="position">The position of the chunk to load.</param>
        /// <returns>The loaded chunk if successful; otherwise, null.</returns>
        public static Chunk LoadChunk(Vector3 position)
        {
            try
            {
                string filePath = GetChunkFilePath(position);
                if (!File.Exists(filePath))
                {
                    return null;
                }

                // Read JSON from file
                string jsonString = File.ReadAllText(filePath);

                // Deserialize JSON to ChunkData
                ChunkData data = JsonSerializer.Deserialize<ChunkData>(jsonString);

                if (data == null)
                {
                    return null;
                }

                // Reconstruct Blocks from Block IDs
                Block[,,] loadedBlocks = ReconstructBlocksFrom1D(data.BlockIds);

                // Reconstruct Position from separate coordinates
                Vector3 loadedPosition = new Vector3(data.PositionX, data.PositionY, data.PositionZ);

                // Create and return the loaded chunk
                Chunk chunk = new Chunk(loadedPosition, loadedBlocks, isLoaded: true);
                Debug.Log($"Chunk loaded from {filePath}");
                return chunk;
            }
            catch (Exception ex)
            {
                Debug.Error($"Error loading chunk: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generates the file path for a chunk based on its position.
        /// </summary>
        /// <param name="position">The position of the chunk.</param>
        /// <returns>The file path where the chunk is saved.</returns>
        private static string GetChunkFilePath(Vector3 position)
        {
            return Path.Combine(SaveDirectory, $"chunk_{position.X}_{position.Y}_{position.Z}.json");
        }

        /// <summary>
        /// Extracts Block IDs from the Blocks array and flattens them into a one-dimensional array.
        /// The order is x, then y, then z (nested loops).
        /// </summary>
        /// <param name="blocks">The 3D Blocks array of the chunk.</param>
        /// <returns>A one-dimensional array of Block IDs.</returns>
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

        /// <summary>
        /// Reconstructs the Blocks array from a one-dimensional array of Block IDs.
        /// The order is x, then y, then z (same as during serialization).
        /// </summary>
        /// <param name="ids">A one-dimensional array of Block IDs.</param>
        /// <returns>The reconstructed 3D Blocks array.</returns>
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

        /// <summary>
        /// A helper class for serialization containing chunk position and block IDs as a one-dimensional array.
        /// </summary>
        private class ChunkData
        {
            public float PositionX { get; set; }
            public float PositionY { get; set; }
            public float PositionZ { get; set; }
            public short[] BlockIds { get; set; }
        }
    }
}
