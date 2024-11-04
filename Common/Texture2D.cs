using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Spacebox.Common
{
    public class Texture2D : IDisposable
    {
        public int Handle { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        private Color4[,] pixels;
        public bool IsReadOnly { get; private set; } = true;

        public Texture2D(int width, int height, bool pixelated = false)
        {
            Width = width;
            Height = height;
            pixels = new Color4[width, height];
            Handle = GL.GenTexture();
            IsReadOnly = false;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    pixels[x, y] = new Color4(1f, 1f, 1f, 1f);
                }
            }

            Use();
            LoadTextureFromPixels();
            SetTextureParameters(pixelated);
        }

        public Texture2D(string path, bool pixelated = false, bool flipY = true)
        {
            Handle = GL.GenTexture();
            Use();

            try
            {
                LoadTextureFromFile(path, flipY);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load texture from {path}: {ex.Message}");
                CreatePinkTexture();
            }

            SetTextureParameters(pixelated);
        }

        private void LoadTextureFromFile(string path, bool flipY)
        {
            using (var image = new Bitmap(path))
            {
                if (flipY)
                    image.RotateFlip(RotateFlipType.RotateNoneFlipY);

                Width = image.Width;
                Height = image.Height;
                pixels = new Color4[Width, Height];

                var data = image.LockBits(
                    new Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb
                );

                unsafe
                {
                    byte* ptr = (byte*)data.Scan0.ToPointer();
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            int index = (y * Width + x) * 4;
                            byte b = ptr[index + 0];
                            byte g = ptr[index + 1];
                            byte r = ptr[index + 2];
                            byte a = ptr[index + 3];
                            pixels[x, y] = new Color4(r / 255f, g / 255f, b / 255f, a / 255f);
                        }
                    }
                }

                image.UnlockBits(data);
            }

            LoadTextureFromPixels();

            GameConsole.Debug("[Texture2D] Loaded: " + path, Color4.DeepPink);
        }

        private void LoadTextureFromPixels()
        {
            byte[] pixelData = new byte[Width * Height * 4];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = (y * Width + x) * 4;
                    Color4 color = pixels[x, y];
                    pixelData[index + 0] = (byte)(color.R * 255);
                    pixelData[index + 1] = (byte)(color.G * 255);
                    pixelData[index + 2] = (byte)(color.B * 255);
                    pixelData[index + 3] = (byte)(color.A * 255);
                }
            }

            GL.TexImage2D(TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                Width,
                Height,
                0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Rgba,
                PixelType.UnsignedByte,
                pixelData
            );
        }

        private void CreatePinkTexture()
        {
            Width = 1;
            Height = 1;
            pixels = new Color4[1, 1];
            pixels[0, 0] = new Color4(1f, 0f, 1f, 1f);
            LoadTextureFromPixels();
        }

        private void SetTextureParameters(bool pixelated)
        {
            var minFilter = pixelated ? TextureMinFilter.Nearest : TextureMinFilter.LinearMipmapLinear;
            var magFilter = pixelated ? TextureMagFilter.Nearest : TextureMagFilter.Linear;

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        public void SetPixelated(bool state)
        {
            SetTextureParameters(state);
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
            IsReadOnly = false;
        }

        public void FlipX()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width / 2; x++)
                {
                    Color4 temp = pixels[x, y];
                    pixels[x, y] = pixels[Width - 1 - x, y];
                    pixels[Width - 1 - x, y] = temp;
                }
            }
            UpdateTexture();
        }

        public void FlipY()
        {
            for (int y = 0; y < Height / 2; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color4 temp = pixels[x, y];
                    pixels[x, y] = pixels[x, Height - 1 - y];
                    pixels[x, Height - 1 - y] = temp;
                }
            }
            UpdateTexture();
        }

        public void UpdateTexture()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Cannot modify a read-only texture.");
            Use();
            LoadTextureFromPixels();
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        public void Use(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }

        public void SetPixelsData(Color4[,] newPixels)
        {
            if (newPixels.GetLength(0) != Width || newPixels.GetLength(1) != Height)
                throw new ArgumentException("Pixel data does not match texture size.");
            pixels = newPixels;
        }

        public Color4[,] GetPixelData()
        {
            return pixels;
        }

        public void Dispose()
        {
            GL.DeleteTexture(Handle);
        }
    }
}
