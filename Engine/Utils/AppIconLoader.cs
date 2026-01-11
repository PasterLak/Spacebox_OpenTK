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

                var iconImage = new OpenTK.Windowing.Common.Input.Image(width, height, pixelBytes);
                window.Icon = new WindowIcon(iconImage);
            }
            catch (Exception ex)
            {
                Debug.Error($"[AppIconLoader] Failed to load icon from {iconPath}: {ex.Message}");
            }
        }

        public static void LoadAndSetCursor(GameWindow window, string cursorPath, int hotspotX = 0, int hotspotY = 0, float scale = 1.0f)
        {
            if (!File.Exists(cursorPath))
            {
                return;
            }

            try
            {
                using var input = File.OpenRead(cursorPath);
                using var originalBitmap = SKBitmap.Decode(input);

                int newWidth = (int)(originalBitmap.Width * scale);
                int newHeight = (int)(originalBitmap.Height * scale);

                if (newWidth < 1) newWidth = 1;
                if (newHeight < 1) newHeight = 1;

                var info = new SKImageInfo(newWidth, newHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var resizedBitmap = originalBitmap.Resize(info, SKSamplingOptions.Default);

                if (resizedBitmap == null)
                {
                    Debug.Error($"[AppIconLoader] Failed to resize cursor: {cursorPath}");
                    return;
                }

                int scaledHotX = (int)(hotspotX * scale);
                int scaledHotY = (int)(hotspotY * scale);

                scaledHotX = Math.Clamp(scaledHotX, 0, newWidth - 1);
                scaledHotY = Math.Clamp(scaledHotY, 0, newHeight - 1);

                byte[] pixelBytes = new byte[newWidth * newHeight * 4];
                int index = 0;

                for (int y = 0; y < newHeight; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        var color = resizedBitmap.GetPixel(x, y);

                        pixelBytes[index++] = color.Red;
                        pixelBytes[index++] = color.Green;
                        pixelBytes[index++] = color.Blue;
                        pixelBytes[index++] = color.Alpha;
                    }
                }

                var cursor = new MouseCursor(scaledHotX, scaledHotY, newWidth, newHeight, pixelBytes);
                window.Cursor = cursor; 
            }
            catch (Exception ex)
            {
                Debug.Error($"[AppIconLoader] Failed to load cursor from {cursorPath}: {ex.Message}");
            }
        }

    }
}