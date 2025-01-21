using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game.Generation
{
    public class LightManager
    {
        private const byte Size = Chunk.Size;
        private readonly Block[,,] _blocks;

        private const bool EnableLighting = true;

        public LightManager(Block[,,] blocks)
        {
            _blocks = blocks;
        }

        public void PropagateLight()
        {
            if (!EnableLighting) return;

            ResetLightLevels();

            Queue<Vector3Byte> lightQueue = new Queue<Vector3Byte>();

            EnqueueLightSources(lightQueue);

            while (lightQueue.Count > 0)
            {
                Vector3Byte pos = lightQueue.Dequeue();
                Block lightSource = _blocks[pos.X, pos.Y, pos.Z];
                float lightLvl = lightSource.LightLevel;

                if (lightLvl <= 0.1f)
                    continue;

                for (byte i = 0; i < AdjacentOffsets.Length; i++)
                {
                    Vector3SByte offset = AdjacentOffsets[i];
                    int nx = pos.X + offset.X;
                    int ny = pos.Y + offset.Y;
                    int nz = pos.Z + offset.Z;

                    if (!IsInRange(nx, ny, nz))
                        continue;

                    Block neighbor = _blocks[nx, ny, nz];

                    if (!(neighbor.IsAir() || neighbor.IsTransparent)) continue;

                    const float attenuation = 0.8f;
                    float newLightLevel = lightLvl * attenuation;

                    Vector3 newLightColor = lightSource.LightColor * attenuation;

                    if (newLightLevel > neighbor.LightLevel)
                    {
                        neighbor.LightLevel = newLightLevel;
                        neighbor.LightColor = newLightColor;
                        lightQueue.Enqueue(new Vector3Byte(nx, ny, nz));
                    }
                    else if (MathF.Abs(newLightLevel - neighbor.LightLevel) < 0.01f)
                    {
                        neighbor.LightColor = (neighbor.LightColor + newLightColor) * 0.5f;

                        _blocks[nx, ny, nz] = neighbor;
                    }
                }
            }
        }


        private void ResetLightLevels()
        {
            for (byte x = 0; x < Size; x++)
                for (byte y = 0; y < Size; y++)
                    for (byte z = 0; z < Size; z++)
                    {
                        Block block = _blocks[x, y, z];
                        if (block.LightLevel < 15f)
                        {
                            block.LightLevel = 0f;
                            block.LightColor = Vector3.Zero;
                        }
                    }
        }

        private void EnqueueLightSources(Queue<Vector3Byte> lightQueue)
        {
            for (byte x = 0; x < Size; x++)
                for (byte y = 0; y < Size; y++)
                    for (byte z = 0; z < Size; z++)
                    {
                        if (_blocks[x, y, z].LightLevel > 0f)
                            lightQueue.Enqueue(new Vector3Byte(x, y, z));
                    }
        }

        private static readonly Vector3SByte[] AdjacentOffsets = new[]
        {
            new Vector3SByte(1, 0, 0),
            new Vector3SByte(-1, 0, 0),
            new Vector3SByte(0, 1, 0),
            new Vector3SByte(0, -1, 0),
            new Vector3SByte(0, 0, 1),
            new Vector3SByte(0, 0, -1),
        };

        private bool IsInRange(int x, int y, int z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        }
    }
}
