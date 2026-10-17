using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Doors;

public class DoorBlock : InteractiveBlock
{
    public DoorBlockData Data { get; }
    public bool IsOpen { get; internal set; }
    public float AutoCloseTimer { get; internal set; }

    public DoorBlock(BlockData blockData) : base(blockData)
    {
        Data = blockData as DoorBlockData;
        HoverTextBlockName = blockData.Name;
    }

    public override void Use(LocalAstronaut player, ref HitInfo hit)
    {
        base.Use(player, ref hit);
        hit.chunk?.SpaceEntity.Doors.Toggle(hit.chunk, hit.blockPositionIndex);
    }

    public void Restore(bool isOpen, float autoCloseTimer)
    {
        IsOpen = isOpen;
        AutoCloseTimer = autoCloseTimer;
    }
}
