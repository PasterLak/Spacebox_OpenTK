using OpenTK.Mathematics;

namespace Spacebox.Game.Resource;

public readonly record struct FaceTexture(int Width, int Height, int Columns, int Rows);

public static class TextureTiling
{
    public const int MaxCellPixels = 4096;

    public static int ResolveCellPixels(IReadOnlyList<FaceTexture> faces, int overridePixels)
    {
        if (overridePixels > 0) return Math.Min(overridePixels, MaxCellPixels);

        int best = 0;
        int bestScore = -1;
        for (int i = 0; i < faces.Count; i++)
        {
            var face = faces[i];
            if (face.Width <= 0 || face.Height <= 0) continue;

            Consider(faces, face.Width / Math.Max(1, face.Columns), ref best, ref bestScore);
            Consider(faces, face.Height / Math.Max(1, face.Rows), ref best, ref bestScore);
        }
        return Math.Max(1, best);
    }

    public static Vector2i GetTiles(int width, int height, int cellPixels)
    {
        int pixels = Math.Max(1, cellPixels);
        return new Vector2i(Math.Max(1, width / pixels), Math.Max(1, height / pixels));
    }

    private static void Consider(IReadOnlyList<FaceTexture> faces, int candidate, ref int best, ref int bestScore)
    {
        if (candidate <= 0) return;

        int score = CountExactFits(faces, candidate);
        if (score > bestScore || (score == bestScore && candidate < best))
        {
            best = candidate;
            bestScore = score;
        }
    }

    private static int CountExactFits(IReadOnlyList<FaceTexture> faces, int cellPixels)
    {
        int fits = 0;
        for (int i = 0; i < faces.Count; i++)
        {
            var face = faces[i];
            bool spans = face.Width == face.Columns * cellPixels && face.Height == face.Rows * cellPixels;
            bool repeats = face.Width == cellPixels && face.Height == cellPixels;
            if (spans || repeats) fits++;
        }
        return fits;
    }
}
