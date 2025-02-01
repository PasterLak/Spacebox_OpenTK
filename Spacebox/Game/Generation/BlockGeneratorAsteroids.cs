using OpenTK.Mathematics;
using Engine;
namespace Spacebox.Game.Generation
{
    public class BlockGeneratorAsteroids : BlockGenerator
    {
        public BlockGeneratorAsteroids(Chunk chunk, Vector3 position) : base(chunk, position)
        {
        }

        public override void Generate()
        {
            GenerateAsteroids();
        }

        private void GenerateAsteroids()
        {
            int sx = _blocks.GetLength(0);
            int sy = _blocks.GetLength(1);
            int sz = _blocks.GetLength(2);

            float rx = sx / 2f;
            float ry = sy / 2f;
            float rz = sz / 2f;
            float radius = MathF.Min(rx, MathF.Min(ry, rz));

            Vector3 center = new Vector3(rx, ry, rz);
            Random random = World.Random;

            for (int x = 0; x < sx; x++)
                for (int y = 0; y < sy; y++)
                    for (int z = 0; z < sz; z++)
                    {
                        Vector3 pos = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                        float dist = Vector3.Distance(pos, center);
                        _blocks[x, y, z] = GameBlocks.CreateBlockFromId(12);
                        float noiseVal = Noise3D(x, y, z, random.Next());
                        float threshold = radius * (0.8f + 0.4f * noiseVal);

                        if (dist <= threshold)
                        {
                            int r = random.Next(0, 11);
                            if (r < 6) _blocks[x, y, z] = GameBlocks.CreateBlockFromId(3);
                            else if (r < 8) _blocks[x, y, z] = GameBlocks.CreateBlockFromId(2);
                            else if (r < 10) _blocks[x, y, z] = GameBlocks.CreateBlockFromId(1);
                            else _blocks[x, y, z] = GameBlocks.CreateBlockFromId(4);
                        }
                        else
                        {
                            _blocks[x, y, z] = GameBlocks.CreateBlockFromId(0);
                        }
                    }
        }

        float Noise3D(int x, int y, int z, int seed)
        {
            int n = x + y * 57 + z * 131 + seed * 999983;
            n = (n << 13) ^ n;
            return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f;
        }
    }
}
