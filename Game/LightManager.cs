using OpenTK.Mathematics;
using Spacebox.Entities;
using System.Collections.Generic;

namespace Spacebox.Game.Lighting
{
    public class LightManager
    {
        private const sbyte Size = 16;
        private readonly Block[,,] _blocks;

        public LightManager(Block[,,] blocks)
        {
            _blocks = blocks;
        }

        public void PropagateLight()
        {
            ResetLightLevels();

            Queue<Vector3i> lightQueue = new Queue<Vector3i>();

            EnqueueLightSources(lightQueue);

            while (lightQueue.Count > 0)
            {
                Vector3i pos = lightQueue.Dequeue();
                Block currentBlock = _blocks[pos.X, pos.Y, pos.Z];
                float lightLevel = currentBlock.LightLevel;

                if (lightLevel <= 0.1f)
                    continue;

                foreach (var offset in GetAdjacentOffsets())
                {
                    int nx = pos.X + offset.X;
                    int ny = pos.Y + offset.Y;
                    int nz = pos.Z + offset.Z;

                    if (!IsInRange(nx, ny, nz))
                        continue;

                    Block neighborBlock = _blocks[nx, ny, nz];

                    if (neighborBlock.Type == BlockType.Air || neighborBlock.IsTransparent)
                    {
                        float attenuation = 0.8f;
                        float newLightLevel = lightLevel * attenuation;

                        Vector3 newLightColor = currentBlock.LightColor * attenuation;

                        if (newLightLevel > neighborBlock.LightLevel)
                        {
                            neighborBlock.LightLevel = newLightLevel;
                            neighborBlock.LightColor = newLightColor;
                            _blocks[nx, ny, nz] = neighborBlock;
                            lightQueue.Enqueue(new Vector3i(nx, ny, nz));
                        }
                        else if (MathF.Abs(newLightLevel - neighborBlock.LightLevel) < 0.01f)
                        {
                            neighborBlock.LightColor = (neighborBlock.LightColor + newLightColor) / 2f;
                            _blocks[nx, ny, nz] = neighborBlock;
                        }
                    }
                }
            }
        }

        private void ResetLightLevels()
        {
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        Block block = _blocks[x, y, z];
                        if (block.LightLevel < 15f)
                        {
                            block.LightLevel = 0f;
                            block.LightColor = Vector3.Zero;
                        }
                    }
        }

        private void EnqueueLightSources(Queue<Vector3i> lightQueue)
        {
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        if (_blocks[x, y, z].LightLevel > 0f)
                            lightQueue.Enqueue(new Vector3i(x, y, z));
                    }
        }

        private List<Vector3i> GetAdjacentOffsets()
        {
            return new List<Vector3i>
            {
                new Vector3i(1, 0, 0),
                new Vector3i(-1, 0, 0),
                new Vector3i(0, 1, 0),
                new Vector3i(0, -1, 0),
                new Vector3i(0, 0, 1),
                new Vector3i(0, 0, -1),
            };
        }

        private bool IsInRange(int x, int y, int z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        }
    }
}
