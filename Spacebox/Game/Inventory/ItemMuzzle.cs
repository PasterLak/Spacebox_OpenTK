using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game;

public readonly record struct ItemMuzzle(Vector2 LowEnd, Vector2 HighEnd, int Width)
{
    public const float FirstPersonPixelSize = 0.004f;

    public static float FirstPersonDepth(Item item) => item.ModelDepth / 500f * 2f;

    public static float WorldDepth(Item item) => 1f / 32f * item.ModelDepth;

    public static bool TryFromPixels(Color4[,] pixels, out ItemMuzzle muzzle)
    {
        muzzle = default;
        if (pixels == null) return false;

        int width = pixels.GetLength(0);
        int height = pixels.GetLength(1);
        int minX = int.MaxValue;
        int maxX = -1;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (pixels[x, y].A < 1f) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }
        }

        if (maxX < 0) return false;

        muzzle = new ItemMuzzle(new Vector2(minX, ColumnCenter(pixels, minX, height)), new Vector2(maxX + 1, ColumnCenter(pixels, maxX, height)), width);
        return true;
    }

    public Vector3 FirstPersonPoint(Vector2 end, Item item) => new Vector3(end.X * FirstPersonPixelSize, end.Y * FirstPersonPixelSize, FirstPersonDepth(item) * 0.5f);

    public Vector3 WorldPoint(Vector2 end, Item item)
    {
        float size = Width > 0 ? 1f / Width : 1f;
        return new Vector3(end.X * size, end.Y * size, WorldDepth(item) * 0.5f);
    }

    public static Vector3 PickForward(Vector3 a, Vector3 b, Vector3 origin, Vector3 forward)
    {
        return Vector3.Dot(a - origin, forward) >= Vector3.Dot(b - origin, forward) ? a : b;
    }

    private static float ColumnCenter(Color4[,] pixels, int x, int height)
    {
        float sum = 0f;
        int count = 0;
        for (int y = 0; y < height; y++)
        {
            if (pixels[x, y].A < 1f) continue;
            sum += y + 0.5f;
            count++;
        }
        return count > 0 ? sum / count : height * 0.5f;
    }
}
