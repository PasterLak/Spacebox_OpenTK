

namespace Spacebox.Game.Generation.Blocks;

public readonly struct PackedBlock
{
    public readonly short Id;
    public readonly Direction Direction;
    public readonly Rotation Rotation;

    public PackedBlock(Block block)
    {
        Id = block.Id;
        Direction = block.Direction;
        Rotation = block.Rotation;
    }

    public Block ToBlock()
    {
        var block = GameAssets.CreateBlockFromId(Id);
        block.SetDirection(Direction);
        block.SetRotation(Rotation);
        return block;
    }
}
