using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Spacebox.Common.Utils
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
                using (Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(iconPath))
                {
                    int width = image.Width;
                    int height = image.Height;
                    byte[] pixels = new byte[width * height * 4];
                    int index = 0;

                    image.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < height; y++)
                        {
                            Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                            for (int x = 0; x < width; x++)
                            {
                                Rgba32 pixel = pixelRow[x];
                                pixels[index++] = pixel.R;
                                pixels[index++] = pixel.G;
                                pixels[index++] = pixel.B;
                                pixels[index++] = pixel.A;
                            }
                        }
                    });

                    var iconImage = new OpenTK.Windowing.Common.Input.Image(width, height, pixels);
                    window.Icon = new WindowIcon(iconImage);
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[AppIconLoader] Failed to load icon from {iconPath}: {ex.Message}");
            }
        }
    }
}