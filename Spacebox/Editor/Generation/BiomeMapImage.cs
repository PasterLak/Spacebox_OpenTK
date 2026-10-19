using OpenTK.Mathematics;

namespace Spacebox.Editor.Generation;

public static class BiomeMapImage
{
    public static Color4[,] BuildSlice(int width, int height, Func<int, int, Color4> colorAt)
    {
        var pixels = new Color4[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
                pixels[x, y] = colorAt(x, y);
        }
        return pixels;
    }

    public static Vector2 ToImage(Vector3 sectorLocal, float sectorSize, Vector2 imageSize)
    {
        return new Vector2(sectorLocal.X / sectorSize * imageSize.X, sectorLocal.Z / sectorSize * imageSize.Y);
    }

    public static Vector3 ToSectorLocal(Vector3 point, Vector3 sectorOrigin, float sectorSize)
    {
        bool isLocal = point.X >= 0f && point.Y >= 0f && point.Z >= 0f && point.X < sectorSize && point.Y < sectorSize && point.Z < sectorSize;
        return isLocal ? point : point - sectorOrigin;
    }

    public static bool IsInSlice(float localY, int slice, int resolution, float sectorSize, int bandCells)
    {
        float cell = localY / sectorSize * resolution;
        return MathF.Abs(cell - slice) <= bandCells;
    }
}
