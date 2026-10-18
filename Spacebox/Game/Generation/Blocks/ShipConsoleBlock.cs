using OpenTK.Mathematics;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using Spacebox.Game.Ships;

namespace Spacebox.Game.Generation.Blocks;

public class ShipConsoleBlock : InteractiveBlock, IShipPart, IEntityBlockListener
{
    private readonly ShipConsoleBlockData _data;

    public SpaceEntity Entity { get; private set; }
    public Vector3i LocalCell { get; private set; }
    public ShipPartFrame Frame { get; private set; }
    public bool IsPowered => IsActive;
    public ShipConsoleBlockData Data => _data;
    public Vector3 PilotForward => -Frame.Front;
    public Vector3 PilotUp => Frame.Up;
    public ConsoleBindings PendingBindings { get; set; }

    public ShipConsoleBlock(BlockData blockData) : base(blockData)
    {
        _data = blockData as ShipConsoleBlockData ?? new ShipConsoleBlockData(blockData, null);
        EFlags = ElectricalFlags.CanConsume;
        ConsumptionRate = _data.Power;
        MaxPower = (short)Math.Clamp(_data.Power * 2, 300, 16000);
        HoverTextBlockName = _data.Weapons ? "Cannon console" : "Ship console";
    }

    public override void Use(LocalAstronaut player, ref HitInfo hit)
    {
        base.Use(player, ref hit);
        if (!IsActive || Entity == null) return;
        ShipPilot.Mount(this, player);
    }

    public void OnAddedToEntity(Chunk chunk, Vector3i localCell)
    {
        this.chunk = chunk;
        if (HasMultiblockOffset) return;

        Entity = chunk.SpaceEntity;
        LocalCell = localCell;
        Frame = ShipPartFrame.Compute(this, localCell);
        Entity.ShipControl.Register(this);

        if (PendingBindings == null) return;
        Entity.ShipControl.SetBindings(localCell, PendingBindings, false);
        PendingBindings = null;
    }

    public void OnRemovedFromEntity()
    {
        Entity?.ShipControl.Unregister(this);
        Entity = null;
    }

    protected override void OnActiveStateChanged(bool isActive)
    {
        base.OnActiveStateChanged(isActive);
        if (!isActive) ShipPilot.OnConsoleUnavailable(this);
    }
}
