using OpenTK.Mathematics;

namespace Spacebox.Game.Generation;
using Engine;
public class BlockGeneratorPoint : BlockGenerator
{
      

    public BlockGeneratorPoint(Chunk chunk, Vector3 position) : base(chunk, position) 
    {
        
    }

    public override void Generate()
    {
        GenerateCross();
    }
    private void GenerateCross()
    {
        Vector3 center = new Vector3(Size / 2f, Size / 2f, Size / 2f);
        float radius = Size / 2f;
        radius = radius * radius;
        Random random = World.Random;

        for (byte x = 0; x < Size; x++)
        for (byte y = 0; y < Size; y++)
        for (byte z = 0; z < Size; z++)
        {
            _blocks[x, y, z] = GameAssets.CreateBlockFromId(0);
            if (y == Size / 2 && z == Size / 2 && x == Size / 2)
            {
                _blocks[x, y, z] = GameAssets.CreateBlockFromId(1);
                continue;
            }
            
                        
                     
        }
    }

        
    
}