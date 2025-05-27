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
            const int maxPreviewSize = 512;

            int width = clientSize.Y;
            int height = clientSize.Y;
            int dropXSize = (clientSize.X - height)/2;

            byte[] pixels = new byte[width * height * 4];
            GL.ReadPixels(dropXSize , 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            Color4[,] colorData = ConvertToColorArray(pixels, width, height);
            Color4[,] finalData;
            DownscaleColor4(colorData, width, height);

            if (width > maxPreviewSize)
            {
                finalData = DownscaleColor4(colorData, maxPreviewSize, maxPreviewSize);

                width = maxPreviewSize;
                height = maxPreviewSize;
            }
            else
            {
                 finalData = colorData;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixelsData(finalData);
            texture.SaveToPng(filePath, false,false);
            texture.Dispose();
        }

        public static Color4[,] DownscaleColor4(Color4[,] input, int newWidth, int newHeight)
        {
            int width = input.GetLength(0);
            int height = input.GetLength(1);

            Color4[,] result = new Color4[newWidth, newHeight];

            float scaleX = width / (float)newWidth;
            float scaleY = height / (float)newHeight;

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    int srcX = (int)(x * scaleX);
                    int srcY = (int)(y * scaleY);

                    result[x, y] = input[srcX, srcY];
                }
            }

            return result;
        }

        public static void SaveGBufferTextures(SceneRenderer renderer, Vector2i clientSize)
        {
            if (!Directory.Exists("GBufferDumps"))
                Directory.CreateDirectory("GBufferDumps");

            var camera = Camera.Main;

            SaveTextureAsPng(renderer.ColorTexture, clientSize, "GBufferDumps/color.png");
            SaveTextureAsPng(renderer.NormalTexture, clientSize, "GBufferDumps/normal.png" );
            SaveTextureAsPng(renderer.DepthTexture, clientSize, "GBufferDumps/depth.png", true);

            Debug.Success("G-Buffer textures saved to GBufferDumps/");
        }

        private static void SaveTextureAsPng(int textureId,
                                     Vector2i size,
                                     string path,
                                     bool isDepth = false,
                                     float nearPlane = 0.1f,
                                     float farPlane = 10f,
                                     bool invert = false)         
        {
            int w = size.X;
            int h = size.Y;
            byte[] rgba = new byte[w * h * 4];

            GL.BindTexture(TextureTarget.Texture2D, textureId);

            if (isDepth)
            {
                float[] z = new float[w * h];
                GL.GetTexImage(TextureTarget.Texture2D, 0,
                               PixelFormat.DepthComponent, PixelType.Float, z);

                for (int i = 0; i < z.Length; i++)
                {
                    // linear depth in world units
                    float ndc = z[i] * 2f - 1f;
                    float linZ = (2f * nearPlane * farPlane) /
                                 (farPlane + nearPlane - ndc * (farPlane - nearPlane));

                    float v = linZ / farPlane;   // 0..1     
                    if (invert) v = 1f - v;    

                    byte g = (byte)MathF.Round(Math.Clamp(v, 0f, 1f) * 255f);
                    int p = i * 4;
                    rgba[p + 0] = g;
                    rgba[p + 1] = g;
                    rgba[p + 2] = g;
                    rgba[p + 3] = 255;
                }
            }
            else
            {
                GL.GetTexImage(TextureTarget.Texture2D, 0,
                               PixelFormat.Rgba, PixelType.UnsignedByte, rgba);
            }

            Color4[,] colors = ConvertToColorArray(rgba, w, h);
            using var tex = new Texture2D(w, h);
            tex.SetPixelsData(colors);
            tex.SaveToPng(path);
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
