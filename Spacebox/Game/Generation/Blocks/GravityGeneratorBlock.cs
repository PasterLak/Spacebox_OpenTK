using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Physics.Gravity;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public class GravityGeneratorBlock : InteractiveBlock, IGravitySource, IEntityBlockListener, INetworkStateBlock
{
    private readonly GravityGeneratorBlockData _data;
    private readonly GravityRoom _room = new GravityRoom();
    private Vector3i _localCell;
    private bool _isPowerDirty;

    public SpaceEntity Entity { get; private set; }
    public Vector3i LocalCell => _localCell;
    public Vector3 WorldCenter { get; private set; }
    public Vector3 WorldDirection { get; private set; }
    public int RadiusSetting { get; private set; }
    public float Radius => RadiusSetting;
    public float Strength { get; private set; }
    public bool IsGravityEnabled { get; private set; } = true;
    public bool InsideOnly { get; private set; }
    public bool IsEmitting => IsActive && IsGravityEnabled && Entity != null;
    public GravityGeneratorBlockData Data => _data;
    public FallDamageJSON FallDamage => _data.GetFallDamage(Strength);
    public GravityRoom Room => _room;
    public ushort RequiredPower => _data.CalculatePower(RadiusSetting, Strength);
    public bool IsDefaultConfiguration => RadiusSetting == _data.Radius && Strength == _data.Strength && IsGravityEnabled && !InsideOnly;

    public GravityGeneratorBlock(BlockData blockData) : base(blockData)
    {
        _data = blockData as GravityGeneratorBlockData ?? new GravityGeneratorBlockData(blockData, null);
        EFlags = ElectricalFlags.CanConsume;
        RadiusSetting = _data.Radius;
        Strength = _data.Strength;
        UpdateConsumption();
        _isPowerDirty = false;
    }

    public override void Use(LocalAstronaut player, ref HitInfo hit)
    {
        base.Use(player, ref hit);
        GravityGeneratorUI.Open(this, player);
    }

    public void Configure(int radius, float strength, bool isGravityEnabled, bool insideOnly)
    {
        radius = Math.Clamp(radius, _data.MinRadius, _data.MaxRadius);
        strength = Math.Clamp(strength, _data.MinStrength, _data.MaxStrength);
        if (radius == RadiusSetting && strength == Strength && isGravityEnabled == IsGravityEnabled && insideOnly == InsideOnly) return;

        if (radius != RadiusSetting || insideOnly != InsideOnly) _room.Invalidate();

        RadiusSetting = radius;
        Strength = strength;
        IsGravityEnabled = isGravityEnabled;
        InsideOnly = insideOnly;

        UpdateConsumption();
        if (chunk != null) chunk.IsModified = true;
        Spacebox.Network.BlockStateNetwork.MarkDirty(Entity, _localCell, this);
    }

    public void WriteNetworkState(Engine.Networking.NetWriter writer)
    {
        writer.Write(RadiusSetting);
        writer.Write(Strength);
        writer.Write(IsGravityEnabled);
        writer.Write(InsideOnly);
    }

    public void ReadNetworkState(Engine.Networking.NetReader reader)
    {
        int radius = reader.ReadInt();
        float strength = reader.ReadFloat();
        bool enabled = reader.ReadBool();
        bool insideOnly = reader.ReadBool();
        Configure(radius, strength, enabled, insideOnly);
        ApplyPendingPower();
    }

    public void ApplyPendingPower()
    {
        if (!_isPowerDirty || Entity == null) return;

        _isPowerDirty = false;
        Entity.ElectricManager.UpdateBlock((_localCell.X, _localCell.Y, _localCell.Z));
    }

    public void UpdateRoom()
    {
        if (InsideOnly && Entity != null)
            _room.Update(Entity, _localCell, RadiusSetting, _data.MaxRoomCells);
    }

    public bool Affects(Vector3 worldPosition)
    {
        if (!InsideOnly) return true;

        UpdateRoom();
        return _room.Contains(Entity, worldPosition);
    }

    public void OnAddedToEntity(Chunk chunk, Vector3i localCell)
    {
        this.chunk = chunk;
        Entity = chunk.SpaceEntity;
        _localCell = localCell;

        var center = Entity.LocalPositionToWorld(new Vector3(localCell.X + 0.5f, localCell.Y + 0.5f, localCell.Z + 0.5f));
        WorldCenter = Entity.ToWorldSpace(center);

        var baseFront = GameAssets.GetBaseFrontDirection(Id);
        var transform = BlockRotationHelper.CalculateTransformMatrix(baseFront, Direction, Rotation);
        var down = Vector3.TransformVector(-Vector3.UnitY, transform).Normalized();
        WorldDirection = Entity.DirectionToWorldSpace(down).Normalized();

        _room.Clear();
        GravityField.Register(this);
    }

    public void OnRemovedFromEntity()
    {
        GravityField.Unregister(this);
        _room.Clear();
        Entity = null;
    }

    private void UpdateConsumption()
    {
        ushort consumption = IsGravityEnabled ? RequiredPower : (ushort)0;
        if (consumption == ConsumptionRate) return;

        ConsumptionRate = consumption;
        MaxPower = (short)Math.Clamp(consumption * 2, 300, 16000);
        _isPowerDirty = true;
    }
}
