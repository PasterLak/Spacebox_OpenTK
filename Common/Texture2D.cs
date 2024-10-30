using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;


namespace Spacebox.Common
{
    public class Texture2D : IDisposable
    {
        public int Handle { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public bool IsReadOnly { get; private set; } = true;

        public Texture2D(string path, bool pixelated = false)
        {
            Handle = GL.GenTexture();
            Use();

            try
            {
                using (var image = new Bitmap(path))
                {
                    image.RotateFlip(RotateFlipType.RotateNoneFlipY);

                    Width = image.Width;
                    Height = image.Height;

                    var data = image.LockBits(
                        new Rectangle(0, 0, image.Width, image.Height),
                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb
                    );

                    GL.TexImage2D(TextureTarget.Texture2D,
                        0,
                        PixelInternalFormat.Rgba,
                        image.Width,
                        image.Height,
                        0,
                        PixelFormat.Bgra,
                        PixelType.UnsignedByte,
                        data.Scan0
                    );

                    image.UnlockBits(data);
                }
            }
            catch
            {
                CreateSpaceTexture();
                
            }

            var minFilter = pixelated ? TextureMinFilter.Nearest : TextureMinFilter.LinearMipmapLinear;
            var magFilter = pixelated ? TextureMagFilter.Nearest : TextureMagFilter.Linear;

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        private void CreatePinkTexture()
        {
            byte[] pinkPixel = { 255, 0, 255, 255 };

            Width = 1;
            Height = 1;

            GL.TexImage2D(TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                1,
                1,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                pinkPixel
            );
        }

        private void CreateSpaceTexture()
        {
            const int size = 1024;
            byte[] pixels = new byte[size * size * 4];

            Width = size;
            Height = size;

            for ( int i = 0; i < pixels.Length; i+=4 )
            {

                Random random = new Random();

                var x = random.Next( 10000 );

                byte color = x < 9995 ? (byte)0 : (byte)random.Next(250);


                pixels[i] = color;
                pixels[i+1] = color;
                pixels[i+2] = color;
                pixels[i+3] = 255;
            }


            GL.TexImage2D(TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                size,
                size,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                pixels
            );
        }

        public void Use(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }

        public void Dispose()
        {
            GL.DeleteTexture(Handle);
        }
    }
}
