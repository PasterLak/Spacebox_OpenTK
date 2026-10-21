namespace Spacebox.Game.Generation.Shapes;

public static class PlateEdges
{
    public const int Left = 1;
    public const int Right = 2;
    public const int Top = 4;
    public const int Bottom = 8;
    public const int All = Left | Right | Top | Bottom;
    public const int VariantCount = All + 1;

    public static int Mask(Vector3Byte size, Vector3Byte cell)
    {
        int mask = 0;
        if (cell.X == 0) mask |= Left;
        if (cell.X >= size.X - 1) mask |= Right;
        if (cell.Z == 0) mask |= Top;
        if (cell.Z >= size.Z - 1) mask |= Bottom;
        return mask;
    }

    public static bool Has(int mask, int edge) => (mask & edge) != 0;
}
