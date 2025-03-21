using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Engine
{
    public class PixelData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] Data { get; set; }
    }

    public enum FilterMode
    {
        Nearest,
        Linear
    }

    public class Texture2D : IResource
    {
        public int Handle { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        private Color4[,] pixels;
        public static bool AllowDebug = true;
        private FilterMode _filterMode = FilterMode.Linear;
        public FilterMode FilterMode
        {
            get => _filterMode;
            set
            {
                _filterMode = value;
                SetTextureParameters();
                UpdateTexture();
            }
        }

        public Texture2D()
        {

        }

        public Texture2D(PixelData pixelData, FilterMode filterMode = FilterMode.Linear)
        {
            Width = pixelData.Width;
            Height = pixelData.Height;
            _filterMode = filterMode;
            Handle = GL.GenTexture();
            Use();
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, pixelData.Data);
            SetTextureParameters();
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }
        public Texture2D(int width, int height, bool pixelated = false)
        {
            Width = width;
            Height = height;
            pixels = new Color4[width, height];
            Handle = GL.GenTexture();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    pixels[x, y] = new Color4(1f, 1f, 1f, 1f);
            Use();
            LoadTextureFromPixels();
            _filterMode = pixelated ? FilterMode.Nearest : FilterMode.Linear;
            SetTextureParameters();
        }
        public Texture2D(string path, bool pixelated = false, bool flipY = true)
        {
            Handle = GL.GenTexture();
            Use();
            try { LoadTextureFromFile(path, flipY); }
            catch (Exception ex)
            {
                Debug.Error($"Failed to load texture from {path}: {ex.Message}");
                CreatePinkTexture();
            }
            _filterMode = pixelated ? FilterMode.Nearest : FilterMode.Linear;
            SetTextureParameters();
        }
        public static Texture2D CreateTexture(int width, int height, Color4 fillColor, FilterMode filterMode = FilterMode.Linear)
        {
            Texture2D texture = new Texture2D(width, height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    texture.pixels[x, y] = fillColor;
            texture.UpdateTexture();
            texture.FilterMode = filterMode;
            return texture;
        }
        private void LoadTextureFromFile(string path, bool flipY)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Texture file not found: {path}");

            using var input = File.OpenRead(path);
            using var codec = SKCodec.Create(input);
            var info = codec.Info;

            var bitmap = new SKBitmap(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            codec.GetPixels(bitmap.Info, bitmap.GetPixels());

            Width = bitmap.Width;
            Height = bitmap.Height;
            pixels = new Color4[Width, Height];

            for (int y = 0; y < Height; y++)
            {
                int srcY = flipY ? Height - 1 - y : y;
                for (int x = 0; x < Width; x++)
                {
                    var color = bitmap.GetPixel(x, srcY);
                    pixels[x, y] = new Color4(
                        color.Red / 255f,
                        color.Green / 255f,
                        color.Blue / 255f,
                        color.Alpha / 255f);
                }
            }

            LoadTextureFromPixels();

            if (AllowDebug)
                Debug.Log("[Texture2D] Loaded from: " + path, Color4.Coral);
        }

        private void LoadTextureFromPixels()
        {
            byte[] pixelData = new byte[Width * Height * 4];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    int index = (y * Width + x) * 4;
                    Color4 color = pixels[x, y];
                    pixelData[index + 0] = (byte)(color.R * 255);
                    pixelData[index + 1] = (byte)(color.G * 255);
                    pixelData[index + 2] = (byte)(color.B * 255);
                    pixelData[index + 3] = (byte)(color.A * 255);
                }
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, pixelData);
        }
        private void CreatePinkTexture()
        {
            Width = 1;
            Height = 1;
            pixels = new Color4[1, 1];
            pixels[0, 0] = new Color4(1f, 0f, 1f, 1f);
            LoadTextureFromPixels();
        }
        private void SetTextureParameters()
        {
            TextureMinFilter minFilter = _filterMode == FilterMode.Nearest ? TextureMinFilter.Nearest : TextureMinFilter.LinearMipmapLinear;
            TextureMagFilter magFilter = _filterMode == FilterMode.Nearest ? TextureMagFilter.Nearest : TextureMagFilter.Linear;
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }
        public Color4 GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException();
            return pixels[x, y];
        }
        public void SetPixel(int x, int y, Color4 color)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException();
            pixels[x, y] = color;
        }
        public void FlipX()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width / 2; x++)
                {
                    Color4 temp = pixels[x, y];
                    pixels[x, y] = pixels[Width - 1 - x, y];
                    pixels[Width - 1 - x, y] = temp;
                }
            UpdateTexture();
        }
        public void FlipY()
        {
            for (int y = 0; y < Height / 2; y++)
                for (int x = 0; x < Width; x++)
                {
                    Color4 temp = pixels[x, y];
                    pixels[x, y] = pixels[x, Height - 1 - y];
                    pixels[x, Height - 1 - y] = temp;
                }
            UpdateTexture();
        }
        public void UpdateTexture()
        {
            Use();
            LoadTextureFromPixels();
            SetTextureParameters();
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }
        public void UpdateTexture(bool pixelated)
        {
            FilterMode = pixelated ? FilterMode.Nearest : FilterMode.Linear;
        }
        public void Use(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }
        public Color4[,] GetPixelData() => pixels;
        public void SetPixelsData(Color4[,] newPixels)
        {
            if (newPixels.GetLength(0) != Width || newPixels.GetLength(1) != Height)
                throw new ArgumentException("Pixel data does not match texture size.");
            pixels = newPixels;
        }
        public void SaveToPng(string path, bool flipY = false)
        {
            if (pixels == null)
            {
                Debug.Error("[Texture2D] No pixel data available to save.");
                return;
            }

            Task.Run(() =>
            {
                using var surface = SKSurface.Create(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul));
                var canvas = surface.Canvas;

                for (int y = 0; y < Height; y++)
                {
                    int targetY = flipY ? Height - 1 - y : y;
                    for (int x = 0; x < Width; x++)
                    {
                        var color = pixels[x, y];
                        var skColor = new SKColor(
                            (byte)(color.R * 255),
                            (byte)(color.G * 255),
                            (byte)(color.B * 255),
                            (byte)(color.A * 255));
                        canvas.DrawPoint(x, targetY, skColor);
                    }
                }

                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.OpenWrite(path);
                data.SaveTo(stream);

                Debug.Success($"Image saved to: {Path.GetFullPath(path)}");
            });
        }


        public static void SavePixelsToPng(string path, Color4[,] pixels, bool flipY = false)
        {
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);

            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;

            for (int y = 0; y < height; y++)
            {
                int targetY = flipY ? height - 1 - y : y;
                for (int x = 0; x < width; x++)
                {
                    var c = pixels[x, y];
                    var skColor = new SKColor(
                        (byte)(c.R * 255),
                        (byte)(c.G * 255),
                        (byte)(c.B * 255),
                        (byte)(c.A * 255));
                    canvas.DrawPoint(x, targetY, skColor);
                }
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(path);
            data.SaveTo(stream);

            Debug.Success($"Image saved to: {Path.GetFullPath(path)}");
        }


        public void Dispose() => GL.DeleteTexture(Handle);

        public IResource Load(string path)
        {
            Handle = GL.GenTexture();
            Use();
            LoadTextureFromFile(path, true);

            _filterMode = FilterMode.Linear;
            SetTextureParameters();

            return this;
        }

    }




    public static class PixelDataLoader
    {
        public static async Task<PixelData> LoadAsync(string path, bool flipY = true)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Texture file not found: {path}");

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
