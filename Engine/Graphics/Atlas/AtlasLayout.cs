namespace Engine.Graphics.Atlas;

public readonly struct AtlasRect
{
    public readonly int X;
    public readonly int Y;
    public readonly int Width;
    public readonly int Height;

    public AtlasRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Overlaps(AtlasRect other)
    {
        return X < other.X + other.Width && other.X < X + Width &&
               Y < other.Y + other.Height && other.Y < Y + Height;
    }
}

public static class AtlasLayout
{
    public static bool Pack(IReadOnlyList<AtlasRect> sizes, int padding, int maxSize, out int width, out int height, AtlasRect[] placements)
    {
        width = 1;
        height = 1;
        if (sizes.Count == 0) return true;

        long area = 0;
        int maxWidth = 1;
        int maxHeight = 1;
        for (int i = 0; i < sizes.Count; i++)
        {
            int w = sizes[i].Width + padding * 2;
            int h = sizes[i].Height + padding * 2;
            area += (long)w * h;
            maxWidth = Math.Max(maxWidth, w);
            maxHeight = Math.Max(maxHeight, h);
        }

        width = NextPowerOfTwo(Math.Max(maxWidth, (int)Math.Ceiling(Math.Sqrt(area))));
        height = NextPowerOfTwo(Math.Max(maxHeight, (int)Math.Ceiling(area / (double)width)));

        while (!TryPack(sizes, padding, width, height, placements))
        {
            if (width <= height) width *= 2;
            else height *= 2;

            if (width > maxSize || height > maxSize)
            {
                width = Math.Min(width, maxSize);
                height = Math.Min(height, maxSize);
                return TryPack(sizes, padding, width, height, placements);
            }
        }

        return true;
    }

    private static bool TryPack(IReadOnlyList<AtlasRect> sizes, int padding, int width, int height, AtlasRect[] placements)
    {
        var packer = new SkylinePacker(width, height);

        for (int i = 0; i < sizes.Count; i++)
        {
            int w = sizes[i].Width;
            int h = sizes[i].Height;
            if (!packer.TryPack(w + padding * 2, h + padding * 2, out int x, out int y))
                return false;

            placements[i] = new AtlasRect(x + padding, y + padding, w, h);
        }

        return true;
    }

    private static int NextPowerOfTwo(int value)
    {
        int result = 1;
        while (result < value) result <<= 1;
        return result;
    }
}
