using OpenTK.Mathematics;

namespace Spacebox.Game.Generation;

public abstract class BlockGenerator
{
    protected const byte Size = Chunk.Size;
    protected readonly Block[,,] _blocks;
    protected readonly Vector3 _position;
        
    public BlockGenerator(Block[,,] blocks, Vector3 position)
    {
        _blocks = blocks;
        _position = position;
    }

    public abstract void Generate();
}