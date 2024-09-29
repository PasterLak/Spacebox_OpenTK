using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Spacebox.Common
{
    public class TextureCube : IDisposable
    {
        public int Handle { get; private set; }

        // Constructor for six textures (one for each face)
        public TextureCube(string[] facePaths, bool pixelated = false)
        {
            if (facePaths.Length != 6)
                throw new ArgumentException("TextureCube requires exactly 6 texture paths.");

            Handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.TextureCubeMap, Handle);

            for (int i = 0; i < 6; i++)
            {
                LoadCubeMapFace(facePaths[i], TextureTarget.TextureCubeMapPositiveX + i);
            }

            SetParameters(pixelated);
        }

        // Constructor for a single texture used for all faces
        public TextureCube(string singleFacePath, bool pixelated = false)
        {
            Handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.TextureCubeMap, Handle);

            for (int i = 0; i < 6; i++)
            {
                LoadCubeMapFace(singleFacePath, TextureTarget.TextureCubeMapPositiveX + i);
            }

            SetParameters(pixelated);
        }

        private void LoadCubeMapFace(string path, TextureTarget face)
        {
            try
            {
                using (var image = new Bitmap(path))
                {
                    // Do not flip the image for cube maps
                    var data = image.LockBits(
                        new Rectangle(0, 0, image.Width, image.Height),
                        ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb
                    );

                    GL.TexImage2D(face,
                        0,
                        PixelInternalFormat.Rgba,
                        image.Width,
                        image.Height,
                        0,
                        OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,
                        PixelType.UnsignedByte,
                        data.Scan0
                    );

                    image.UnlockBits(data);
                }
            }
            catch
            {
                CreatePinkTexture(face);
            }
        }

        private void CreatePinkTexture(TextureTarget face)
        {
            byte[] pinkPixel = { 255, 0, 255, 255 };
            GL.TexImage2D(face,
                0,
                PixelInternalFormat.Rgba,
                1,
                1,
                0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Rgba,
                PixelType.UnsignedByte,
                pinkPixel
            );
        }

        private void SetParameters(bool pixelated)
        {
            var minFilter = pixelated ? TextureMinFilter.Nearest : TextureMinFilter.Linear;
            var magFilter = pixelated ? TextureMagFilter.Nearest : TextureMagFilter.Linear;

            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)magFilter);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
        }

        public void Use(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.TextureCubeMap, Handle);
        }

        public void Dispose()
        {
            GL.DeleteTexture(Handle);
        }
    }
}
