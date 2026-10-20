using OpenTK.Mathematics;
using Spacebox.Game.Resource;
using Spacebox.Game.Ships;

namespace Spacebox.Game.Generation.Blocks;

public class CannonBlock : ElectricalBlock, IShipPart, IEntityBlockListener
{
    private const float MuzzleClearance = 0.3f;
    private const float RestAngle = 0.005f;

    private readonly CannonBlockData _data;
    private Vector3 _targetDirection;
    private float _reload;

    public SpaceEntity Entity { get; private set; }
    public Vector3i LocalCell { get; private set; }
    public ShipPartFrame Frame { get; private set; }
    public bool IsPowered => IsActive;
    public bool TriggerHeld { get; set; }
    public bool IsAiming { get; set; }
    public int ShooterPeer { get; set; } = -1;
    public Vector3 AimDirection { get; private set; } = Vector3.UnitY;
    public string ProjectileName => _data.ProjectileName;
    public CannonBlockData Data => _data;
    public float ReloadProgress => _data.ReloadSeconds > 0f ? 1f - Math.Clamp(_reload / _data.ReloadSeconds, 0f, 1f) : 1f;
    public float Length => Frame.ExtentAlong(Frame.Front) * 2f;
    public Vector3 PivotLocal => Frame.Center - Frame.Front * Frame.ExtentAlong(Frame.Front);
    public Vector3 MuzzleLocal => PivotLocal + AimDirection * (MathF.Max(_data.BarrelLength, Length) + MuzzleClearance);
    public bool IsAtRest => ShipAiming.AngleBetween(AimDirection, Frame.Front) < RestAngle;

    public CannonBlock(BlockData blockData) : base(blockData)
    {
        _data = blockData as CannonBlockData ?? new CannonBlockData(blockData, null);
        EFlags = ElectricalFlags.CanConsume;
        ConsumptionRate = _data.Power;
        MaxPower = (short)Math.Clamp(_data.Power * 2, 300, 16000);
    }

    public void OnAddedToEntity(Chunk chunk, Vector3i localCell)
    {
        IsMeshHidden = false;
        if (HasMultiblockOffset) return;

        Entity = chunk.SpaceEntity;
        LocalCell = localCell;
        Frame = ShipPartFrame.Compute(this, localCell);
        AimDirection = Frame.Front;
        _targetDirection = Frame.Front;
        Entity.ShipControl.Register(this);
    }

    public void OnRemovedFromEntity()
    {
        Entity?.ShipControl.Unregister(this);
        Entity = null;
        TriggerHeld = false;
        IsAiming = false;
    }

    public void SetAimTarget(Vector3 directionLocal)
    {
        _targetDirection = ShipAiming.ClampToCone(Frame.Front, directionLocal, _data.AimCone);
    }

    public void SetAimPoint(Vector3 pointLocal)
    {
        SetAimTarget(pointLocal - PivotLocal);
    }

    public void ResetAim()
    {
        _targetDirection = Frame.Front;
    }

    public bool UpdateCannon(float deltaTime)
    {
        AimDirection = ShipAiming.RotateTowards(AimDirection, _targetDirection, MathHelper.DegreesToRadians(_data.TurnSpeed) * deltaTime);
        if (_reload > 0f) _reload -= deltaTime;

        return TriggerHeld && IsActive && _reload <= 0f;
    }

    public void MarkFired()
    {
        _reload = _data.ReloadSeconds;
    }
}
