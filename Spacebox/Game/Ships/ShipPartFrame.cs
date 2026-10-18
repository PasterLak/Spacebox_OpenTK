using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Ships;

public readonly struct ShipPartFrame
{
    public readonly Vector3 Center;
    public readonly Vector3 Front;
    public readonly Vector3 Up;
    public readonly Vector3 Size;

    public ShipPartFrame(Vector3 center, Vector3 front, Vector3 up, Vector3 size)
    {
        Center = center;
        Front = front;
        Up = up;
        Size = size;
    }

    public static ShipPartFrame Compute(Block block, Vector3i originCell)
    {
        var data = GameAssets.GetBlockDataById(block.Id);
        var transform = MultiblockLayout.GetTransform(data, block.Direction, block.Rotation);
        var size = new Vector3(data.Size.X, data.Size.Y, data.Size.Z);
        var halfExtent = (size - Vector3.One) * 0.5f;

        var center = new Vector3(originCell.X + 0.5f, originCell.Y + 0.5f, originCell.Z + 0.5f) + Vector3.TransformVector(halfExtent, transform);
        var front = Vector3.TransformVector(BlockRotationHelper.GetDirectionVector(data.BaseFrontDirection), transform).Normalized();
        var up = Vector3.TransformVector(Vector3.UnitY, transform).Normalized();
        var worldSize = Vector3.TransformVector(size, transform);

        return new ShipPartFrame(center, front, up, new Vector3(MathF.Abs(worldSize.X), MathF.Abs(worldSize.Y), MathF.Abs(worldSize.Z)));
    }

    public float ExtentAlong(Vector3 direction)
    {
        return (MathF.Abs(direction.X) * Size.X + MathF.Abs(direction.Y) * Size.Y + MathF.Abs(direction.Z) * Size.Z) * 0.5f;
    }
}
