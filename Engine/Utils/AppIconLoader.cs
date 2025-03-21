using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using SkiaSharp;

namespace Engine.Utils
{
    public static class AppIconLoader
    {
        public static void LoadAndSetIcon(GameWindow window, string iconPath)
        {
            if (!File.Exists(iconPath))
            {
                Debug.Error($"[AppIconLoader] Icon file not found: {iconPath}");
                return;
            }

            try
            {
                using var input = File.OpenRead(iconPath);
                using var codec = SKCodec.Create(input);
                var info = codec.Info;

                using var bitmap = new SKBitmap(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                codec.GetPixels(bitmap.Info, bitmap.GetPixels());

                int width = bitmap.Width;
                int height = bitmap.Height;
                byte[] pixelBytes = new byte[width * height * 4];
                int index = 0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // GetPixel(...) often returns a 32-bit color packed as RGBA
                        // (uint c = (uint)bitmap.GetPixel(x, y) in older Skia versions)
                        uint c = (uint)bitmap.GetPixel(x, y);
                        byte r = (byte)((c >> 0) & 0xFF);
                        byte g = (byte)((c >> 8) & 0xFF);
                        byte b = (byte)((c >> 16) & 0xFF);
                        byte a = (byte)((c >> 24) & 0xFF);

                        pixelBytes[index++] = b;
                        pixelBytes[index++] = g;
                        pixelBytes[index++] = r;
                        pixelBytes[index++] = a;
                    }
                }

                var iconImage = new Image(width, height, pixelBytes);
                window.Icon = new WindowIcon(iconImage);
            }
            catch (Exception ex)
            {
                Debug.Error($"[AppIconLoader] Failed to load icon from {iconPath}: {ex.Message}");
            }
        }
    }
}
