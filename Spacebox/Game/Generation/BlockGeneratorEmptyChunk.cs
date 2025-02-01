using OpenTK.Mathematics;
using Engine;
namespace Spacebox.Game.Generation
{

    public class BlockGeneratorEmptyChunk : BlockGenerator
    {

        public BlockGeneratorEmptyChunk(Chunk chunk, Vector3 position) : base(chunk, position)
        {

        }

        public override void Generate()
        {
            GenerateSphereBlocks();
        }
        private void GenerateSphereBlocks()
        {
         

            Random random = World.Random;
       

            for (byte x = 0; x < Size; x++)
                for (byte y = 0; y < Size; y++)
                    for (byte z = 0; z < Size; z++)
                    {
                        _blocks[x, y, z] = GameBlocks.CreateBlockFromId(0);
                    }
        }


    }


}
