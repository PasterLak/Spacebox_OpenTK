using System;
using OpenTK.Mathematics;
using Engine.Graphics.Rhi;
using Engine;

    public class AxesTest : SceneNode, IDisposable
    {
        private MeshBuffer _buffer;
        private Shader _shader;
        private float[] vertices;
        private uint[] indices = new uint[] { 0, 1, 2, 3, 4, 5 };
        public float Length { get; set; }

        public AxesTest(Vector3 position, float length)
        {
            Position = position;
            Length = length;
          
            _shader = Resources.Load<Shader>("Resources/Shaders/axes");
            UpdateVertices();
            SetupBuffer();
        }

        public AxesTest(Vector3 position, float length, Vector3 rotation)
        {
            Position = position;
            Length = length;
           
            _shader = Resources.Load<Shader>("Resources/Shaders/axes");
            UpdateVertices();
            SetupBuffer();
        }

        private void SetupBuffer()
        {
            _buffer = new MeshBuffer(new BufferAttribute[]
            {
                new BufferAttribute { Name = "position", Size = 3 },
                new BufferAttribute { Name = "color", Size = 3 }
            });
            _buffer.BindBuffer(ref vertices, ref indices);
            _buffer.SetAttributes();
        }

        private void UpdateVertices()
        {
            vertices = new float[]
            {
                // X axis
                0.0f, 0.0f, 0.0f,  1.0f, 0.0f, 0.0f,
                Length, 0.0f, 0.0f,  1.0f, 0.0f, 0.0f,

                // Y axis
                0.0f, 0.0f, 0.0f,  0.0f, 1.0f, 0.0f,
                0.0f, Length, 0.0f,  0.0f, 1.0f, 0.0f,

                // Z axis
                0.0f, 0.0f, 0.0f,  0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, Length,  0.0f, 0.0f, 1.0f,
            };

            if (_buffer != null)
            {
                _buffer.UploadVertices(vertices, BufferUsage.Dynamic);
            }
        }

        public void Render(Camera camera)
        {
            Render(camera.GetViewMatrix(), camera.GetProjectionMatrix());
        }

        public void Render(Matrix4 view, Matrix4 projection)
        {
            _shader.Use();
            Matrix4 model = WorldMatrix;

            _shader.SetMatrix4("model", model, false);
            _shader.SetMatrix4("view", view, false);
            _shader.SetMatrix4("projection", projection, false);

            _buffer.DrawIndexed(PrimitiveTopology.Lines, indices.Length);
        }

        public void SetPosition(Vector3 newPosition)
        {
            Position = newPosition;
            UpdateVertices();
        }

        public void SetLength(float newLength)
        {
            Length = newLength;
            UpdateVertices();
        }

        public void SetRotation(Vector3 newRotation)
        {
            SetRotation(newRotation);
            UpdateVertices();
        }

    public void Dispose()
    {
        _buffer.Dispose();
    }
}

