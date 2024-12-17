using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using OpenTK.Graphics.OpenGL4;

namespace Spacebox.Common
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
                    {
                        Directory.Delete("Frames", true);
                    }
                }
                else
                {
                    Debug.Success("Frame was captured!");
                }
            }
        }

        public static int FrameNumber = 0;
       

        public static void SaveFrame()
        {
            if (!_isActive)
                return;

            if (!Directory.Exists("Frames"))
            {
                Directory.CreateDirectory("Frames");
            }

            int width = Window.Instance.Size.X;
            int height = Window.Instance.Size.Y;
            string filePath = $"Frames/frame{FrameNumber}.png";

            byte[] pixels = new byte[width * height * 4];
            GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

            Task.Run(() =>
            {
                using (Image<Rgba32> image = new Image<Rgba32>(width, height))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int glIndex = ((y * width) + x) * 4;
                            int imgY = height - y - 1;
                            image[x, imgY] = new Rgba32(pixels[glIndex], pixels[glIndex + 1], pixels[glIndex + 2], pixels[glIndex + 3]);
                        }
                    }
                    image.Save(filePath, new PngEncoder());
                }
               
            });

            FrameNumber++;
        }

        public static void SaveScreenshot()
        {
          

            if (!Directory.Exists("Screenshots"))
            {
                Directory.CreateDirectory("Screenshots");
            }

            int width = Window.Instance.Size.X;
            int height = Window.Instance.Size.Y;
            string filePath = $"Screenshots/screenshot_{DateTime.Now.Year}_{DateTime.Now.Month}_{DateTime.Now.Day}_{DateTime.Now.Hour}_{DateTime.Now.Minute}_{DateTime.Now.Second}.png";

            byte[] pixels = new byte[width * height * 4];
            GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

            Task.Run(() =>
            {
                using (Image<Rgba32> image = new Image<Rgba32>(width, height))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int glIndex = ((y * width) + x) * 4;
                            int imgY = height - y - 1;
                            image[x, imgY] = new Rgba32(pixels[glIndex], pixels[glIndex + 1], pixels[glIndex + 2], pixels[glIndex + 3]);
                        }
                    }
                    image.Save(filePath, new PngEncoder());
                }
                Debug.Success($"Screenshot saved: {Path.GetFullPath(filePath)}");
            });

            
        }

        public static async Task<byte[]> CaptureFrameAsPngAsync(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and Height must be positive integers.");

            byte[] pixels = new byte[width * height * 4];
            GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

            return await Task.Run(() =>
            {
                using (Image<Rgba32> image = new Image<Rgba32>(width, height))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int glIndex = ((y * width) + x) * 4;
                            int imgY = height - y - 1;
                            image[x, imgY] = new Rgba32(pixels[glIndex], pixels[glIndex + 1], pixels[glIndex + 2], pixels[glIndex + 3]);
                        }
                    }
                    using (var ms = new MemoryStream())
                    {
                        image.Save(ms, new PngEncoder());
                        return ms.ToArray();
                    }
                }
            });
        }
    }
}
