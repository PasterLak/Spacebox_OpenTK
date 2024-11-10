using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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

        public static void SaveChunk(Chunk chunk)
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
                    PositionX = chunk.Position.X,
                    PositionY = chunk.Position.Y,
                    PositionZ = chunk.Position.Z,
                    BlockIds = blockIds,
                    Directions = directions
                };
                string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                string filePath = GetChunkFilePath(chunk.Position);
                File.WriteAllText(filePath, jsonString);
                Debug.Log($"Chunk saved at {filePath}");
            }
            catch (Exception ex)
            {
                Debug.Error($"Error saving chunk: {ex.Message}");
            }
        }

        public static Chunk LoadChunk(Vector3 position)
        {
            try
            {
                string filePath = GetChunkFilePath(position);
                if (!File.Exists(filePath))
                {
                    return null;
                }
                string jsonString = File.ReadAllText(filePath);
                ChunkData data = JsonSerializer.Deserialize<ChunkData>(jsonString);
                if (data == null)
                {
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
                Vector3 loadedPosition = new Vector3(data.PositionX, data.PositionY, data.PositionZ);
                Chunk chunk = new Chunk(loadedPosition, loadedBlocks, isLoaded: true);
                Debug.Success($"Chunk loaded from {filePath}");
                return chunk;
            }
            catch (Exception ex)
            {
                Debug.Error($"Error loading chunk: {ex.Message}");
                return null;
            }
        }

        private static string GetChunkFilePath(Vector3 position)
        {
            return Path.Combine(SaveDirectory, $"chunk_{position.X}_{position.Y}_{position.Z}.json");
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
    }
}
