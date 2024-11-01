using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.GUI
{
    public class Sprite : IDisposable
    {
        private int _vao;
        private int _vbo;
        private int _ebo;
        private Shader _shader;

        private float[] _vertices;
        private readonly uint[] _indices = {
            0, 1, 3,   // Первый треугольник
            1, 2, 3    // Второй треугольник
        };

        private Vector2 _position;
        private Vector2 _size;

        private int _windowWidth;
        private int _windowHeight;

        private Texture2D _texture;

        // Событие для отписки
        private bool _isDisposed = false;

        public Sprite(Texture2D texture, Vector2 position, Vector2 size, Shader shader = null)
        {
            _texture = texture;
            _position = position;
            _size = size;
            _windowWidth = Window.Instance.Size.X;
            _windowHeight = Window.Instance.Size.Y;

            _shader = shader ?? new Shader("Shaders/sprite");

            Initialize();
        }

        public Sprite(string imagePath, Vector2 position, Vector2 size, bool pixelated = false, Shader shader = null)
        {
            _texture = new Texture2D(imagePath, pixelated);
            _position = position;
            _size = size;
            _windowWidth = Window.Instance.Size.X;
            _windowHeight = Window.Instance.Size.Y;

            _shader = shader ?? new Shader("Shaders/sprite");

            Initialize();
        }

        private void Initialize()
        {
            SetVertices();

         
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

            // Позиции вершин
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            // Текстурные координаты
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            GL.BindVertexArray(0);

            // Обработчик события изменения размера окна
            Window.OnResized += UpdateWindowSize;
        }

        private void SetVertices()
        {
            // Конвертируем пиксельные позиции в координаты OpenGL
            float left = _position.X;
            float right = _position.X + _size.X;
            float top = _position.Y;
            float bottom = _position.Y + _size.Y;

            // Нормализуем в координаты OpenGL (-1 до 1)
            float normalizedLeft = (left / _windowWidth) * 2.0f - 1.0f;
            float normalizedRight = (right / _windowWidth) * 2.0f - 1.0f;
            float normalizedTop = 1.0f - (top / _windowHeight) * 2.0f;
            float normalizedBottom = 1.0f - (bottom / _windowHeight) * 2.0f;

            _vertices = new float[]
            {
                // Позиции          // Текстурные координаты
                normalizedRight, normalizedTop,     1.0f, 1.0f, // Верхний правый
                normalizedRight, normalizedBottom,  1.0f, 0.0f, // Нижний правый
                normalizedLeft,  normalizedBottom,  0.0f, 0.0f, // Нижний левый
                normalizedLeft,  normalizedTop,      0.0f, 1.0f  // Верхний левый
            };
        }

        public void UpdatePosition(Vector2 newPosition)
        {
            _position = newPosition;
            UpdateVertices();
        }

        public void UpdateSize(Vector2 newSize)
        {
            _size = newSize;
            UpdateVertices();
        }

        private void UpdateVertices()
        {
            SetVertices();

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertices.Length * sizeof(float), _vertices);
        }

        private void UpdateWindowSize(Vector2 newWindowSize)
        {
            _windowWidth = (int)newWindowSize.X;
            _windowHeight = (int)newWindowSize.Y;
            UpdateVertices();
        }

        public void SetTexture(Texture2D newTexture)
        {
            _texture = newTexture;
        }

        public void SetShader(Shader newShader)
        {
            _shader?.Dispose();
            _shader = newShader;
        }

        public void Render(Vector2? offset = null, Vector2? tiling = null)
        {
            _shader.Use();

            // Устанавливаем uniforms
            _shader.SetVector2("offset", offset ?? Vector2.Zero);
            _shader.SetVector2("tiling", tiling ?? Vector2.One);

            // Привязываем текстуру
            _texture.Use(TextureUnit.Texture0);
            _shader.SetInt("spriteTexture", 0);

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public Vector2 Position
        {
            get => _position;
            set => UpdatePosition(value);
        }

        public Vector2 Size
        {
            get => _size;
            set => UpdateSize(value);
        }

        public Texture2D Texture
        {
            get => _texture;
            set => SetTexture(value);
        }

        public Shader Shader
        {
            get => _shader;
            set => SetShader(value);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                GL.DeleteVertexArray(_vao);
                GL.DeleteBuffer(_vbo);
                GL.DeleteBuffer(_ebo);
                _texture?.Dispose();
                _shader?.Dispose();

                Window.OnResized -= UpdateWindowSize;

                _isDisposed = true;
            }
        }
    }
}
