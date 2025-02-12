using Spacebox.Game.Generation;

namespace Spacebox.Generation
{
    internal class ChunkLOD
    {
        public const byte SIZE = Chunk.Size; // 32
        public enum LOD : byte
        {
            Near = 16,
            Far = 8
        }

        public bool[,,] CreateLOD(Block[,,] blocks, LOD step)
        {
            byte lodResolution = (byte)step;
            bool[,,] lodData = new bool[lodResolution, lodResolution, lodResolution];
            int cellSize = SIZE / lodResolution;
            int totalBlocks = cellSize * cellSize * cellSize;
            byte threshold = (byte)(totalBlocks / 2);

            for (byte i = 0; i < lodResolution; i++)
            {
                for (byte j = 0; j < lodResolution; j++)
                {
                    for (byte k = 0; k < lodResolution; k++)
                    {
                        int solidCount = 0;
                        byte startX = (byte)(i * cellSize);
                        byte startY = (byte)(j * cellSize);
                        byte startZ = (byte)(k * cellSize);
                        bool isSolid = false;
                        for (byte x = startX; x < startX + cellSize; x++)
                        {
                            for (byte y = startY; y < startY + cellSize; y++)
                            {
                                for (byte z = startZ; z < startZ + cellSize; z++)
                                {
                                    if (!blocks[x, y, z].IsAir)
                                    {
                                        if (++solidCount > threshold)
                                        {
                                            isSolid = true;
                                            goto EndCell;
                                        }
                                    }
                                }
                            }
                        }
                    EndCell:
                        lodData[i, j, k] = isSolid;
                    }
                }
            }
            return lodData;
        }
    }
}
