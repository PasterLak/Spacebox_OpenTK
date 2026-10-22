using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using SkiaSharp;

namespace Engine.Utils
{
    public static class AppIconLoader
    {
        public static readonly int[] WindowIconSizes = { 16, 24, 32, 48, 64, 256 };

        public static WindowIcon CreateWindowIcon(string iconPath, IReadOnlyList<int> sizes)
        {
            if (!File.Exists(iconPath))
            {
                Debug.Error($"[AppIconLoader] Icon file not found: {iconPath}");
                return null;
            }

            try
            {
                using var source = SKBitmap.Decode(iconPath);
                if (source == null)
                {
                    Debug.Error($"[AppIconLoader] Icon file cannot be read: {iconPath}");
                    return null;
                }

                var images = new OpenTK.Windowing.Common.Input.Image[sizes.Count];
                for (int i = 0; i < sizes.Count; i++) images[i] = ToImage(source, sizes[i]);
                return new WindowIcon(images);
            }
            catch (Exception ex)
            {
                Debug.Error($"[AppIconLoader] Failed to load icon from {iconPath}: {ex.Message}");
                return null;
            }
        }

        private static OpenTK.Windowing.Common.Input.Image ToImage(SKBitmap source, int size)
        {
            var info = new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using var resized = source.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell));
            byte[] pixels = new byte[size * size * 4];
            int index = 0;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var color = resized.GetPixel(x, y);
                    pixels[index++] = color.Red;
                    pixels[index++] = color.Green;
                    pixels[index++] = color.Blue;
                    pixels[index++] = color.Alpha;
                }
            }
            return new OpenTK.Windowing.Common.Input.Image(size, size, pixels);
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