using OpenTK.Mathematics;
using Spacebox.Game.Physics.Gravity;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public class GravityGeneratorBlock : ElectricalBlock, IGravitySource, IEntityBlockListener
{
    private readonly GravityGeneratorBlockData _data;

    public SpaceEntity Entity { get; private set; }
    public Vector3 WorldCenter { get; private set; }
    public Vector3 WorldDirection { get; private set; }
    public float Radius => _data?.Radius ?? 32f;
    public float Strength => _data?.Strength ?? 9.81f;
    public bool IsEmitting => IsActive && Entity != null;

    public GravityGeneratorBlock(BlockData blockData) : base(blockData)
    {
        _data = blockData as GravityGeneratorBlockData;
        EFlags = ElectricalFlags.CanConsume;
        ConsumptionRate = _data?.Power ?? 200;
        MaxPower = (short)Math.Clamp(ConsumptionRate * 2, 300, 16000);
    }

    public void OnAddedToEntity(Chunk chunk, Vector3i localCell)
    {
        Entity = chunk.SpaceEntity;

        var center = Entity.LocalPositionToWorld(new Vector3(localCell.X + 0.5f, localCell.Y + 0.5f, localCell.Z + 0.5f));
        WorldCenter = Entity.ToWorldSpace(center);

        var baseFront = GameAssets.GetBaseFrontDirection(Id);
        var transform = BlockRotationHelper.CalculateTransformMatrix(baseFront, Direction, Rotation);
        var down = Vector3.TransformVector(-Vector3.UnitY, transform).Normalized();
        WorldDirection = Entity.DirectionToWorldSpace(down).Normalized();

        GravityField.Register(this);
    }

    public void OnRemovedFromEntity()
    {
        GravityField.Unregister(this);
        Entity = null;
    }
}
