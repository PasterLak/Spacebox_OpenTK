using Engine.Graphics.Rhi;
using OpenTK.Mathematics;
using Engine;
using Engine.Graphics;

namespace Engine.GUI
{
    public class Sprite : IDisposable
    {
        private VertexArrayHandle _vao;
        private BufferHandle _vbo;
        private BufferHandle _ebo;
        private Shader _shader;

        private float[] _vertices;
        private readonly uint[] _indices = {
            0, 1, 3,
            1, 2, 3
        };

        private Vector2 _position;
        private Vector2 _size;

        private int _windowWidth;
        private int _windowHeight;

        private Texture2D _texture;

        private bool _isDisposed = false;

        public Sprite(Texture2D texture, Vector2 position, Vector2 size, Shader shader = null)
        {
            _texture = texture;
            _position = position;
            _size = size;
           // _windowWidth = Window.Instance.Size.X;
           // _windowHeight = Window.Instance.Size.Y;

            _shader = shader ?? Resources.Load<Shader>("Resources/Shaders/sprite");

            Initialize();
        }

        public Sprite(string imagePath, Vector2 position, Vector2 size, bool pixelated = false, Shader shader = null)
        {
            _texture = Resources.Load<Texture2D>(imagePath);
            _texture.FilterMode = FilterMode.Nearest;
            _position = position;
            _size = size;
            // _windowWidth = Window.Instance.Size.X;
            // _windowHeight = Window.Instance.Size.Y;

            _shader = shader ?? Resources.Load<Shader>("Resources/Shaders/sprite");

            Initialize();
        }

        private void Initialize()
        {
            SetVertices();


            var device = Gpu.Device;
            _vao = device.CreateVertexArray();
            _vbo = device.CreateBuffer();
            _ebo = device.CreateBuffer();

            GLState.Blend(true);
            GLState.BlendFunc(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);

            device.BindVertexArray(_vao);

            device.BufferData(BufferKind.Vertex, _vbo, new ReadOnlySpan<float>(_vertices), BufferUsage.Dynamic);

            device.BufferData(BufferKind.Index, _ebo, new ReadOnlySpan<uint>(_indices), BufferUsage.Static);

            device.BindBuffer(BufferKind.Vertex, _vbo);
            // pos
            device.SetVertexAttribute(0, 2, VertexComponentType.Float, false, 4 * sizeof(float), 0);

            // uv
            device.SetVertexAttribute(1, 2, VertexComponentType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            device.BindVertexArray(VertexArrayHandle.None);


           // Spacebox.Window.Instance.OnResized += UpdateWindowSize;
        }

        private void SetVertices()
        {

            float left = _position.X;
            float right = _position.X + _size.X;
            float top = _position.Y;
            float bottom = _position.Y + _size.Y;


            float normalizedLeft = left / _windowWidth * 2.0f - 1.0f;
            float normalizedRight = right / _windowWidth * 2.0f - 1.0f;
            float normalizedTop = 1.0f - top / _windowHeight * 2.0f;
            float normalizedBottom = 1.0f - bottom / _windowHeight * 2.0f;

            _vertices = new float[]
            {

                normalizedRight, normalizedTop,     1.0f, 1.0f,
                normalizedRight, normalizedBottom,  1.0f, 0.0f,
                normalizedLeft,  normalizedBottom,  0.0f, 0.0f,
                normalizedLeft,  normalizedTop,      0.0f, 1.0f
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

            Gpu.Device.BufferSubData(BufferKind.Vertex, _vbo, 0, new ReadOnlySpan<float>(_vertices));
        }

        public void UpdateWindowSize(Vector2 newWindowSize)
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
            Resources.Release(_shader);
            _shader = newShader;
        }

        public void Render(Vector2? offset = null, Vector2? tiling = null)
        {
            _shader.Use();


            _shader.SetVector2("offset", offset ?? Vector2.Zero);
            _shader.SetVector2("tiling", tiling ?? Vector2.One);


            _texture.Use(0);
            _shader.SetInt("spriteTexture", 0);
           // GL.BindTexture(TextureTarget.Texture2D, _texture.Handle);

            var device = Gpu.Device;
            device.BindVertexArray(_vao);
            device.DrawIndexed(PrimitiveTopology.Triangles, _indices.Length, IndexType.UnsignedInt, 0);
            device.BindVertexArray(VertexArrayHandle.None);
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
                var device = Gpu.Device;
                device.DeleteVertexArray(_vao);
                device.DeleteBuffer(_vbo);
                device.DeleteBuffer(_ebo);
                Resources.Release(_texture);
                Resources.Release(_shader);

               //Window.OnResized -= UpdateWindowSize;

                _isDisposed = true;
            }
        }
    }
}
