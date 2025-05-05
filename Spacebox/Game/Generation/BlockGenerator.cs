using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
namespace Spacebox.Game.Generation;

public abstract class BlockGenerator
{
    protected const byte Size = Chunk.Size;
    protected readonly Chunk _chunk;
    protected readonly Block[,,] _blocks;
    protected readonly Vector3 _position;
        
    public BlockGenerator(Chunk chunk, Vector3 position)
    {
        _chunk = chunk;  
        _blocks = chunk.Blocks;
        _position = position;
    }

    public abstract void Generate();
}