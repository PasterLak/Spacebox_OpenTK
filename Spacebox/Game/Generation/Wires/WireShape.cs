using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation.Wires;

public readonly record struct WireFace(WireTexture Texture, int QuarterTurns);

public static class WireShape
{
    public const int DirectionCount = 6;

    private const int EdgeTop = 0;
    private const int EdgeRight = 1;
    private const int EdgeBottom = 2;
    private const int EdgeLeft = 3;

    public static readonly IReadOnlyDictionary<Direction, Vector3> FaceVectors =
        BlockRotationHelper.GetCachedFaceVectors(Direction.Up, Direction.Up, Rotation.None);

    private static readonly Direction[] FaceUp = new Direction[DirectionCount];
    private static readonly Direction[] FaceRight = new Direction[DirectionCount];

    static WireShape()
    {
        for (int i = 0; i < DirectionCount; i++)
        {
            var face = (Direction)i;
            FaceUp[i] = ToDirection(BlockRotationHelper.GetFaceUp(FaceVectors, face));
            FaceRight[i] = ToDirection(BlockRotationHelper.GetFaceRight(FaceVectors, face));
        }
    }

    public static int Bit(Direction direction) => 1 << (int)direction;

    public static Direction Opposite(Direction direction) => direction switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        Direction.Forward => Direction.Back,
        _ => Direction.Forward
    };

    public static WireFace Resolve(int connections, Direction face, Direction restAxis)
    {
        if ((connections & Bit(face)) != 0) return new WireFace(WireTexture.Port, 0);

        int count = CountBits(connections);
        if (count >= 3) return new WireFace(WireTexture.Hub, 0);

        if (count == 2 && TryGetPerpendicularPair(connections, out var a, out var b))
            return ResolveAngle(face, a, b);

        var axis = count == 0 ? restAxis : FirstDirection(connections);
        if (face == axis || face == Opposite(axis)) return new WireFace(WireTexture.Port, 0);
        return Straight(face, axis);
    }

    private static WireFace ResolveAngle(Direction face, Direction a, Direction b)
    {
        if (face == Opposite(a)) return Straight(face, b);
        if (face == Opposite(b)) return Straight(face, a);

        int edgeA = EdgeOf(face, a);
        int edgeB = EdgeOf(face, b);
        int turns = (edgeA + 1) % 4 == edgeB ? edgeA : edgeB;
        return new WireFace(WireTexture.Angle, turns);
    }

    private static WireFace Straight(Direction face, Direction axis)
    {
        var up = FaceUp[(int)face];
        bool vertical = axis == up || axis == Opposite(up);
        return new WireFace(WireTexture.Straight, vertical ? 0 : 1);
    }

    private static int EdgeOf(Direction face, Direction direction)
    {
        var up = FaceUp[(int)face];
        var right = FaceRight[(int)face];
        if (direction == up) return EdgeTop;
        if (direction == right) return EdgeRight;
        if (direction == Opposite(up)) return EdgeBottom;
        return EdgeLeft;
    }

    private static bool TryGetPerpendicularPair(int connections, out Direction a, out Direction b)
    {
        a = FirstDirection(connections);
        b = FirstDirection(connections & ~Bit(a));
        return b != Opposite(a);
    }

    private static Direction FirstDirection(int connections)
    {
        for (int i = 0; i < DirectionCount; i++)
            if ((connections & (1 << i)) != 0) return (Direction)i;
        return Direction.Up;
    }

    private static int CountBits(int value)
    {
        int count = 0;
        for (int i = 0; i < DirectionCount; i++)
            if ((value & (1 << i)) != 0) count++;
        return count;
    }

    private static Direction ToDirection(Vector3 vector)
    {
        if (MathF.Abs(vector.X) > 0.5f) return vector.X > 0 ? Direction.Right : Direction.Left;
        if (MathF.Abs(vector.Y) > 0.5f) return vector.Y > 0 ? Direction.Up : Direction.Down;
        return vector.Z > 0 ? Direction.Forward : Direction.Back;
    }
}
