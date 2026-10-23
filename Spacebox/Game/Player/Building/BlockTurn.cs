using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Player.Building;

public static class BlockTurn
{
    public static readonly Matrix4 QuarterTurn = Matrix4.CreateRotationY(MathHelper.PiOver2);

    public static void Turn(Direction baseFront, Direction direction, Rotation rotation, out Direction turnedDirection, out Rotation turnedRotation)
    {
        var target = BlockRotationHelper.CalculateTransformMatrix(baseFront, direction, rotation) * QuarterTurn;
        turnedDirection = direction;
        turnedRotation = rotation;
        float best = float.MaxValue;

        for (byte d = 0; d < 6; d++)
        {
            for (byte r = 0; r < 4; r++)
            {
                float error = Difference(BlockRotationHelper.CalculateTransformMatrix(baseFront, (Direction)d, (Rotation)r), target);
                if (error >= best) continue;
                best = error;
                turnedDirection = (Direction)d;
                turnedRotation = (Rotation)r;
            }
        }
    }

    public static Vector3i TurnOffset(Vector3i offset, Vector3i size)
    {
        var center = new Vector3(offset.X + 0.5f - size.X * 0.5f, offset.Y + 0.5f - size.Y * 0.5f, offset.Z + 0.5f - size.Z * 0.5f);
        var turned = Vector3.TransformVector(center, QuarterTurn);
        var turnedSize = TurnSize(size);
        return new Vector3i(
            (int)MathF.Round(turned.X + turnedSize.X * 0.5f - 0.5f),
            (int)MathF.Round(turned.Y + turnedSize.Y * 0.5f - 0.5f),
            (int)MathF.Round(turned.Z + turnedSize.Z * 0.5f - 0.5f));
    }

    public static Vector3i TurnSize(Vector3i size) => new Vector3i(size.Z, size.Y, size.X);

    private static float Difference(Matrix4 a, Matrix4 b)
    {
        float sum = 0f;
        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
                sum += MathF.Abs(a[row, column] - b[row, column]);
        return sum;
    }
}
