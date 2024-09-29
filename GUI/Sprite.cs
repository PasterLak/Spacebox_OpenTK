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

        private Texture2D _texture;

        public Sprite(string imagePath, Vector2 position, Vector2 size, int windowWidth, int windowHeight, bool pixelated = false)
        {
            _shader = new Shader("Shaders/sprite");

            _position = position;
            _size = size;
            _windowWidth = windowWidth;
            _windowHeight = windowHeight;

            // Load the texture using Texture2D
            _texture = new Texture2D(imagePath, pixelated);

            // Define vertices with position and size
            SetVertices();

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
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            // Texture coordinates
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            GL.BindVertexArray(0);

            // Event handlers for window resize (if applicable)
            Window.OnResized += UpdateWindowSize;
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

        public void UpdateWindowSize(Vector2 newWindowSize)
        {
            _windowWidth = (int)newWindowSize.X;
            _windowHeight = (int)newWindowSize.Y;
            SetVertices();

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertices.Length * sizeof(float), _vertices);
        }

        public Shader GetShader() => _shader;

        public void Render(Vector2 offset, Vector2 tiling)
        {
            _shader.Use();

            // Set offset and tiling uniforms
            _shader.SetVector2("offset", offset);
            _shader.SetVector2("tiling", tiling);

            // Bind texture
            _texture.Use(TextureUnit.Texture0);
            _shader.SetInt("spriteTexture", 0);

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            _texture.Dispose();
            _shader.Dispose();
        }
    }
}
