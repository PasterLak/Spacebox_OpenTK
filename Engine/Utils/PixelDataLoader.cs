using SkiaSharp;

namespace Engine.Utils
{
    public static class PixelDataLoader
    {
        public static async Task<PixelData> LoadAsync(string path, bool flipY = true)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"[Texture2D] Texture file not found: {path}");

            return await Task.Run(() =>
            {
                using var input = File.OpenRead(path);
                using var codec = SKCodec.Create(input);
                var info = codec.Info;

                var bitmap = new SKBitmap(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                codec.GetPixels(bitmap.Info, bitmap.GetPixels());

                int width = bitmap.Width;
                int height = bitmap.Height;
                byte[] data = new byte[width * height * 4];

                for (int y = 0; y < height; y++)
                {
                    int row = flipY ? height - 1 - y : y;
                    for (int x = 0; x < width; x++)
                    {
                        var color = bitmap.GetPixel(x, row);
                        int index = (y * width + x) * 4;
                        data[index + 0] = color.Red;
                        data[index + 1] = color.Green;
                        data[index + 2] = color.Blue;
                        data[index + 3] = color.Alpha;
                    }
                }

                return new PixelData { Width = width, Height = height, Data = data };
            });
        }
    }


}
