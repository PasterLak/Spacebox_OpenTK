using OpenTK.Mathematics;
using SkiaSharp;

namespace Engine.Graphics.Atlas;

public static class ImagePixels
{
    public static readonly string[] SupportedExtensions = { ".png", ".jpg", ".jpeg" };

    public static bool TryLoadTopDown(string path, out Color4[,] pixels)
    {
        pixels = null;
        if (!File.Exists(path)) return false;

        try
        {
            using var input = File.OpenRead(path);
            using var codec = SKCodec.Create(input);
            if (codec == null) return false;

            var info = codec.Info;
            using var bitmap = new SKBitmap(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            codec.GetPixels(bitmap.Info, bitmap.GetPixels());

            pixels = new Color4[info.Width, info.Height];
            for (int y = 0; y < info.Height; y++)
            {
                for (int x = 0; x < info.Width; x++)
                {
                    var color = bitmap.GetPixel(x, y);
                    pixels[x, y] = new Color4(color.Red / 255f, color.Green / 255f, color.Blue / 255f, color.Alpha / 255f);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.Error($"[ImagePixels] Failed to load {path}: {ex.Message}");
            return false;
        }
    }

    public static bool TryGetSize(string path, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (!File.Exists(path)) return false;

        try
        {
            using var input = File.OpenRead(path);
            using var codec = SKCodec.Create(input);
            if (codec == null) return false;

            width = codec.Info.Width;
            height = codec.Info.Height;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static Color4[,] FlipY(Color4[,] source)
    {
        int width = source.GetLength(0);
        int height = source.GetLength(1);
        var result = new Color4[width, height];

        for (int y = 0; y < height; y++)
        {
            int sourceY = height - 1 - y;
            for (int x = 0; x < width; x++)
            {
                result[x, y] = source[x, sourceY];
            }
        }

        return result;
    }

    public static Color4[,] Copy(Color4[,] source)
    {
        return (Color4[,])source.Clone();
    }
}
