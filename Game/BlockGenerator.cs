using OpenTK.Mathematics;
using Spacebox.Entities;
using System;

namespace Spacebox.Game.Generation
{
    public class BlockGenerator
    {
        private const sbyte Size = 16;
        private readonly Block[,,] _blocks;
        private readonly Vector3 _position;

        public BlockGenerator(Block[,,] blocks, Vector3 position)
        {
            _blocks = blocks;
            _position = position;
        }

        public void GenerateSphereBlocks()
        {
            Vector3 center = new Vector3(Size / 2f, Size / 2f, Size / 2f);
            float radius = Size / 2f;

            Random random = new Random();

            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        Vector3 blockPosition = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                        float distance = Vector3.Distance(blockPosition, center);

                        if (distance <= radius)
                        {
                            int r = random.Next(0, 10);
                            if (r < 8)
                            {
                                _blocks[x, y, z] = distance < radius / 2f
                                    ? GameBlocks.CreateFromId(2)
                                    : GameBlocks.CreateFromId(1);
                            }
                            else if (r == 8)
                            {
                                _blocks[x, y, z] = distance < radius / 2f
                                    ? GameBlocks.CreateFromId(4)
                                    : GameBlocks.CreateFromId(3);
                            }
                            else if (r == 9)
                            {
                                _blocks[x, y, z] = distance < radius / 2f
                                    ? GameBlocks.CreateFromId(2)
                                    : GameBlocks.CreateFromId(5);
                            }
                        }
                        else
                        {
                            _blocks[x, y, z] = GameBlocks.CreateFromId(0);
                        }
                    }
        }
    }
}
