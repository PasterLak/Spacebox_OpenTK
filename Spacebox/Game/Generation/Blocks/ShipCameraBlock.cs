using OpenTK.Mathematics;
using Spacebox.Game.Resource;
using Spacebox.Game.Ships;

namespace Spacebox.Game.Generation.Blocks;

public class ShipCameraBlock : ElectricalBlock, IShipPart, IEntityBlockListener
{
    private const float ViewOffset = 0.15f;

    private readonly ShipCameraBlockData _data;

    public SpaceEntity Entity { get; private set; }
    public Vector3i LocalCell { get; private set; }
    public ShipPartFrame Frame { get; private set; }
    public bool IsPowered => IsActive;
    public Vector3 ViewPointLocal => Frame.Center + Frame.Front * (Frame.ExtentAlong(Frame.Front) + ViewOffset);

    public ShipCameraBlock(BlockData blockData) : base(blockData)
    {
        _data = blockData as ShipCameraBlockData ?? new ShipCameraBlockData(blockData, null);
        EFlags = ElectricalFlags.CanConsume;
        ConsumptionRate = _data.Power;
        MaxPower = (short)Math.Clamp(_data.Power * 2, 300, 16000);
    }

    public void OnAddedToEntity(Chunk chunk, Vector3i localCell)
    {
        if (HasMultiblockOffset) return;

        Entity = chunk.SpaceEntity;
        LocalCell = localCell;
        Frame = ShipPartFrame.Compute(this, localCell);
        Entity.ShipControl.Register(this);
    }

    public void OnRemovedFromEntity()
    {
        Entity?.ShipControl.Unregister(this);
        Entity = null;
    }
}
