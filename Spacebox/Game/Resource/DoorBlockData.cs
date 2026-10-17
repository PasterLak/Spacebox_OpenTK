using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Resource;

public class DoorBlockData : BlockData
{
    public Direction MoveDirection { get; }
    public float Speed { get; }
    public float OpenTime { get; }
    public bool AutoClose { get; }

    public DoorBlockData(BlockData baseBlock, DoorJSON settings) : base(baseBlock)
    {
        settings ??= new DoorJSON();

        if (!Enum.TryParse(settings.MoveDirection, true, out Direction direction))
        {
            Debug.Error($"[DoorBlockData] Unknown door move direction <{settings.MoveDirection}> for <{baseBlock.Name}>, using Right");
            direction = Direction.Right;
        }

        MoveDirection = direction;
        Speed = Math.Max(0.1f, settings.Speed);
        OpenTime = Math.Max(0f, settings.OpenTime);
        AutoClose = settings.AutoClose;
    }

    public Vector3 ModelSlide
    {
        get
        {
            var axis = BlockRotationHelper.GetDirectionVector(MoveDirection);
            float distance = MathF.Abs(axis.X) * Size.X + MathF.Abs(axis.Y) * Size.Y + MathF.Abs(axis.Z) * Size.Z;
            return axis * distance;
        }
    }

    public float SlideDistance => ModelSlide.Length;
}
