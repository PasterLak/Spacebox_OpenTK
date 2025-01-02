using OpenTK.Mathematics;

namespace Spacebox.Game.Generation
{
   
    public class BlockGeneratorSphere : BlockGenerator
    {

        public BlockGeneratorSphere(Chunk chunk, Vector3 position) : base(chunk, position)
        {
           
        }

        public override void Generate()
        {
            GenerateSphereBlocks();
        }
        private void GenerateSphereBlocks()
        {
            Vector3 center = new Vector3(Size / 2f, Size / 2f, Size / 2f);
            float radius = Size / 2f ;
           
            Random random = World.Random;
            radius = radius * radius;


            for (byte x = 0; x < Size; x++)
                for (byte y = 0; y < Size; y++)
                    for (byte z = 0; z < Size; z++)
                    {
                        Vector3 blockPosition = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                        float distance = Vector3.DistanceSquared(blockPosition, center);

            
                        if (distance <= radius)
                        {
                            int r = random.Next(0, 11);
                         
                            if (r < 8)
                            {
                              
                                    if (distance < radius / 4f)
                                        _blocks[x, y, z] = GameBlocks.CreateBlockFromId(3);
                                    else if (distance < radius / 2f)
                                        _blocks[x, y, z] = GameBlocks.CreateBlockFromId(2);
                                    else
                                        _blocks[x, y, z] = GameBlocks.CreateBlockFromId(1);
                               

                            }
                            else if (r == 8)
                            {
                                if (distance < radius / 4f)
                                    _blocks[x, y, z] = GameBlocks.CreateBlockFromId(8);
                                else if (distance < radius / 2f)
                                    _blocks[x, y, z] = GameBlocks.CreateBlockFromId(7);
                                else
                                    _blocks[x, y, z] = GameBlocks.CreateBlockFromId(6);
                            }
                            else if (r == 9)
                            {

                                if (distance < radius / 4f)
                                    _blocks[x, y, z] = GameBlocks.CreateBlockFromId(3);
                                else if (distance < radius / 2f)
                                    _blocks[x, y, z] = GameBlocks.CreateBlockFromId(2);
                                else
                                    _blocks[x, y, z] = GameBlocks.CreateBlockFromId(5);

                            }
                            else if (r == 10)
                            {

                                if (distance < radius / 4f)
                                    _blocks[x, y, z] = GameBlocks.CreateBlockFromId(3);
                                else if (distance < radius / 2f)
                                    _blocks[x, y, z] = GameBlocks.CreateBlockFromId(2);
                                else
                                    _blocks[x, y, z] = GameBlocks.CreateBlockFromId(4);
                            }
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
