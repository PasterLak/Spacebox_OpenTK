using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.GUI
{
    public class Sprite
    {
        private readonly int _vao;
        private readonly int _vbo;
        private readonly int _ebo;
        private readonly int _texture;
        private readonly Shader _shader;

        private float[] _vertices;
        private readonly uint[] _indices = {
            0, 1, 3,   // First Triangle
            1, 2, 3    // Second Triangle
        };

        public Sprite(string imagePath, Vector2 position, Vector2 size)
        {
            _shader = new Shader("Shaders/sprite");

            // Define vertices with position and size
            SetVertices(position, size);

            // Load the texture
            _texture = LoadTexture(imagePath);

            // Generate buffers
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

            // Vertex positions
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Texture coordinates
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        private int LoadTexture(string path)
        {
            int textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            using (Bitmap bitmap = new Bitmap(path))
            {
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                bitmap.UnlockBits(data);
            }

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            return textureId;
        }

        private void SetVertices(Vector2 position, Vector2 size)
        {
            float left = position.X - size.X / 2;
            float right = position.X + size.X / 2;
            float top = position.Y + size.Y / 2;
            float bottom = position.Y - size.Y / 2;

            _vertices = new float[]
            {
                // Positions     // Texture Coords
                right, top,     1.0f, 1.0f, // Top Right
                right, bottom,  1.0f, 0.0f, // Bottom Right
                left, bottom,   0.0f, 0.0f, // Bottom Left
                left, top,      0.0f, 1.0f  // Top Left 
            };
        }

        public void UpdatePosition(Vector2 newPosition, Vector2 newSize)
        {
            SetVertices(newPosition, newSize);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);
        }

        public void Render()
        {
            _shader.Use();
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }
    }
}
