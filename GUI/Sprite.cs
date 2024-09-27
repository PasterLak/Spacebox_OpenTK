using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
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

        private Vector2 _position;
        private Vector2 _size;

        private int _windowWidth;
        private int _windowHeight;

        public Sprite(string imagePath, Vector2 position, Vector2 size, int windowWidth, int windowHeight)
        {
            _shader = new Shader("Shaders/sprite");

            _position = position;
            _size = size;
            _windowWidth = windowWidth;
            _windowHeight = windowHeight;

            // Define vertices with position and size
            SetVertices();

            Window.OnResized += UpdateWindowSize;
            Window.OnResized += UpdateSize;
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
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

            // Vertex positions
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Texture coordinates
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

      

        private int LoadTexture(string path)
        {
            int textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            using (Bitmap bitmap = new Bitmap(path))
            {
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    bitmap.Width,
                    bitmap.Height,
                    0,
                    OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,
                    PixelType.UnsignedByte,
                    data.Scan0);

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

        private void SetVertices()
        {
            // Convert pixel positions to OpenGL coordinates
            float left = _position.X;
            float right = _position.X + _size.X;
            float top = _position.Y;
            float bottom = _position.Y + _size.Y;

            // Normalize to OpenGL coordinates (-1 to 1)
            float normalizedLeft = (left / _windowWidth) * 2.0f - 1.0f;
            float normalizedRight = (right / _windowWidth) * 2.0f - 1.0f;
            float normalizedTop = 1.0f - (top / _windowHeight) * 2.0f;
            float normalizedBottom = 1.0f - (bottom / _windowHeight) * 2.0f;

            _vertices = new float[]
            {
                // Positions          // Texture Coords
                normalizedRight, normalizedTop,     1.0f, 1.0f, // Top Right
                normalizedRight, normalizedBottom,  1.0f, 0.0f, // Bottom Right
                normalizedLeft,  normalizedBottom,  0.0f, 0.0f, // Bottom Left
                normalizedLeft,  normalizedTop,      0.0f, 1.0f  // Top Left
            };
        }

        public void UpdatePosition(Vector2 newPosition)
        {
            _position = newPosition;
            SetVertices();

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertices.Length * sizeof(float), _vertices);
        }

        public void UpdateSize(Vector2 newSize)
        {
            _size = newSize;
            SetVertices();

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertices.Length * sizeof(float), _vertices);
        }

        public void UpdateWindowSize(Vector2 v)
        {
            UpdateWindowSize((int)v.X, (int)v.Y);
        }

        public void UpdateWindowSize(int newWindowWidth, int newWindowHeight)
        {
            _windowWidth = newWindowWidth;
            _windowHeight = newWindowHeight;
            SetVertices();

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertices.Length * sizeof(float), _vertices);
        }

        public Shader GetShader() => _shader;

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
