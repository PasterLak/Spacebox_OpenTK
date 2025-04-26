using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;

namespace Engine
{
    public class FramebufferCapture
    {
        private static bool _isActive = false;
        public static bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                FrameNumber = 0;
                if (value)
                {
                    if (Directory.Exists("Frames"))
                        Directory.Delete("Frames", true);
                }
                else
                {
                    Debug.Success("Frame was captured!");
                }
            }
        }

        public static int FrameNumber = 0;

        public static void SaveFrame(GameWindow window)
        {
            if (!_isActive)
                return;

            if (!Directory.Exists("Frames"))
                Directory.CreateDirectory("Frames");

            string filePath = $"Frames/frame{FrameNumber}.png";
            SaveFrameUsingTexture(window, filePath);
            FrameNumber++;
        }

        public static void SaveScreenshot(GameWindow window)
        {
            SaveScreenshot(window.ClientSize);
        }

        public static void SaveScreenshot(Vector2i clientSize)
        {
            if (!Directory.Exists("Screenshots"))
                Directory.CreateDirectory("Screenshots");

            string filePath = $"Screenshots/screenshot_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.png";
            SaveScreenshotUsingTexture(clientSize, filePath);
            Debug.Success($"Screenshot saved: {Path.GetFullPath(filePath)}");
        }

        public static void SaveFrameUsingTexture(GameWindow window, string filePath)
        {
            int width = window.Size.X;
            int height = window.Size.Y;
            byte[] pixels = new byte[width * height * 4];
            GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            Color4[,] colorData = ConvertToColorArray(pixels, width, height);
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixelsData(colorData);
            texture.SaveToPng(filePath);
            texture.Dispose();
        }

        public static void SaveScreenshotUsingTexture(Vector2i clientSize, string filePath)
        {
            int width = clientSize.X;
            int height = clientSize.Y;
            byte[] pixels = new byte[width * height * 4];
            GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            Color4[,] colorData = ConvertToColorArray(pixels, width, height);
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixelsData(colorData);
            texture.SaveToPng(filePath);
            texture.Dispose();
        }

        public static void SaveWorldPreview(Vector2i clientSize, string filePath)
        {
            int width = clientSize.Y;
            int height = clientSize.Y;
            int dropXSize = (clientSize.X - height)/2;

            byte[] pixels = new byte[width * height * 4];
            GL.ReadPixels(dropXSize , 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            Color4[,] colorData = ConvertToColorArray(pixels, width, height);
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixelsData(colorData);
            texture.SaveToPng(filePath);
            texture.Dispose();
        }

        public static async Task<byte[]> CaptureFrameAsPngAsync(int width, int height)
        {
            byte[] pixels = new byte[width * height * 4];
            GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            Color4[,] colorData = ConvertToColorArray(pixels, width, height);
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixelsData(colorData);
            string tempPath = Path.Combine(Path.GetTempPath(), "temp_capture.png");
            texture.SaveToPng(tempPath);
            texture.Dispose();
            return await Task.Run(() => File.ReadAllBytes(tempPath));
        }

        private static Color4[,] ConvertToColorArray(byte[] pixels, int width, int height)
        {
            Color4[,] result = new Color4[width, height];
            for (int y = 0; y < height; y++)
            {
                int flippedY = height - 1 - y;
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width + x) * 4;
                    float r = pixels[index + 0] / 255f;
                    float g = pixels[index + 1] / 255f;
                    float b = pixels[index + 2] / 255f;
                    float a = pixels[index + 3] / 255f;
                    result[x, flippedY] = new Color4(r, g, b, a);
                }
            }
            return result;
        }
    }
}
