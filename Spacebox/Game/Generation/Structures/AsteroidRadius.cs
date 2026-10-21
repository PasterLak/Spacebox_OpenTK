namespace Spacebox.Game.Generation.Structures;

public static class AsteroidRadius
{
    public const int Min = 2;
    public const int Max = SpaceEntity.SizeBlocksHalf;

    public static int Clamp(int radius) => Math.Clamp(radius, Min, Max);
}
